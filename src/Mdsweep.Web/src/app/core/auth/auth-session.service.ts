import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApplicationError } from '../errors/application-error';
import { ApiClient } from '../api/api-client';

export type TenantSession = {
  appUserId: string;
  displayName: string;
  tenantId: string;
  roles: Array<'Administrator' | 'Dispatcher' | 'Driver'>;
};

export type AvailableTenant = {
  id: string;
  name: string;
  roles: TenantSession['roles'];
};

export type SessionBootstrap = {
  userId: string;
  displayName: string;
  activeTenant: AvailableTenant | null;
  availableTenants: AvailableTenant[];
};

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  readonly #api = inject(ApiClient);
  readonly #document = inject(DOCUMENT);

  async establish(): Promise<SessionBootstrap> {
    try {
      return await firstValueFrom(this.#api.http.get<SessionBootstrap>(this.#api.url('auth/session')));
    } catch (error) {
      if (error instanceof ApplicationError && error.status === 401) {
        this.signIn();
      }
      throw error;
    }
  }

  async selectTenant(tenantId: string): Promise<void> {
    await firstValueFrom(this.#api.http.post<void>(this.#api.url('auth/tenant-context'), { tenantId }));
  }

  toTenantSession(session: SessionBootstrap): TenantSession | null {
    if (session.activeTenant === null) {
      return null;
    }

    return {
      appUserId: session.userId,
      displayName: session.displayName,
      tenantId: session.activeTenant.id,
      roles: session.activeTenant.roles,
    };
  }

  signIn(): void {
    const returnUrl = `${window.location.pathname}${window.location.search}${window.location.hash}`;
    window.location.replace(`${this.#api.url('auth/login')}?returnUrl=${encodeURIComponent(returnUrl)}`);
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
