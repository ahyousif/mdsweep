import { DOCUMENT } from '@angular/common';
import { DestroyRef, inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApplicationError } from '../errors/application-error';
import { ApiClient } from '../api/api-client';

export type TenantSession = {
  appUserId: string;
  displayName: string;
  tenantId: string;
  tenantName?: string;
  roles: Array<'Administrator' | 'Dispatcher' | 'Driver'>;
};

export type AvailableTenant = {
  id: string;
  name: string;
  roles: TenantSession['roles'];
};

export type SessionBootstrap = {
  userId: string | null;
  displayName: string;
  activeTenant: AvailableTenant | null;
  availableTenants: AvailableTenant[];
};

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  readonly #api = inject(ApiClient);
  readonly #document = inject(DOCUMENT);

  constructor() {
    const window = this.#document.defaultView;
    const tenantChanged = (event: StorageEvent) => {
      // The BFF cookie is shared by tabs; discard their old Tenant state together.
      if (event.key === 'mdsweep.tenant') window?.location.reload();
    };
    window?.addEventListener('storage', tenantChanged);
    inject(DestroyRef).onDestroy(() => window?.removeEventListener('storage', tenantChanged));
  }

  async availableSessions(): Promise<TenantSession[]> {
    const session = await firstValueFrom(
      this.#api.http.get<SessionBootstrap>(this.#api.url('auth/session')),
    );
    if (session.userId === null) return [];
    return session.availableTenants.map((tenant) => ({
      appUserId: session.userId!,
      displayName: session.displayName,
      tenantId: tenant.id,
      tenantName: tenant.name,
      roles: tenant.roles,
    }));
  }

  async switchTenant(tenantId: string): Promise<void> {
    await this.selectTenant(tenantId);
    this.#document.defaultView?.localStorage.setItem('mdsweep.tenant', tenantId);
    this.#document.defaultView?.location.assign('/');
  }

  async establish(): Promise<SessionBootstrap> {
    try {
      const session = await firstValueFrom(
        this.#api.http.get<SessionBootstrap>(this.#api.url('auth/session')),
      );
      // An invitation may create the first membership after the original OIDC login.
      if (session.activeTenant === null && session.availableTenants.length === 1) {
        await this.selectTenant(session.availableTenants[0].id);
        return await firstValueFrom(
          this.#api.http.get<SessionBootstrap>(this.#api.url('auth/session')),
        );
      }
      return session;
    } catch (error) {
      if (error instanceof ApplicationError && error.status === 401) {
        this.signIn();
      }
      throw error;
    }
  }

  async selectTenant(tenantId: string): Promise<void> {
    await firstValueFrom(
      this.#api.http.post<void>(this.#api.url('auth/tenant-context'), { tenantId }),
    );
  }

  toTenantSession(session: SessionBootstrap): TenantSession | null {
    if (session.activeTenant === null || session.userId === null) {
      return null;
    }

    return {
      appUserId: session.userId,
      displayName: session.displayName,
      tenantId: session.activeTenant.id,
      tenantName: session.activeTenant.name,
      roles: session.activeTenant.roles,
    };
  }

  signIn(): void {
    const returnUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
    window.location.replace(
      `${this.#api.url('auth/login')}?returnUrl=${encodeURIComponent(returnUrl)}`,
    );
  }

  signOut(): void {
    const form = this.#document.createElement('form');
    form.method = 'post';
    form.action = this.#api.url('auth/logout');

    const token = this.#document.createElement('input');
    token.type = 'hidden';
    token.name = '__RequestVerificationToken';
    token.value = this.readCookie('XSRF-TOKEN');
    form.append(token);

    this.#document.body.append(form);
    this.#document.defaultView?.localStorage.removeItem('mdsweep.tenant');
    form.submit();
  }

  private readCookie(name: string): string {
    const value = this.#document.cookie
      .split('; ')
      .find((cookie) => cookie.startsWith(`${name}=`))
      ?.slice(name.length + 1);

    return value === undefined ? '' : decodeURIComponent(value);
  }
}
