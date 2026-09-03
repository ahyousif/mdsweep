import { DOCUMENT } from '@angular/common';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../api/api-client';

export type TenantSession = {
  appUserId: string;
  displayName: string;
  tenantId: string;
  roles: Array<'Administrator' | 'Dispatcher' | 'Driver'>;
};

type Membership = {
  userId: string;
  firstName: string;
  lastName: string;
  tenantId: string;
  role: TenantSession['roles'][number];
};

type AntiforgeryResponse = {
  token: string;
};

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  readonly #api = inject(ApiClient);
  readonly #document = inject(DOCUMENT);

  async establish(): Promise<TenantSession> {
    const memberships = await firstValueFrom(
      this.#api.http.get<Membership[]>(this.#api.url('auth/me')),
    );

    const sessions = new Map<string, TenantSession>();

    for (const membership of memberships) {
      const session = sessions.get(membership.tenantId) ?? {
        appUserId: membership.userId,
        displayName: `${membership.firstName} ${membership.lastName}`.trim(),
        tenantId: membership.tenantId,
        roles: [],
      };

      session.roles.push(membership.role);
      sessions.set(membership.tenantId, session);
    }

    const availableSessions = [...sessions.values()];

    if (availableSessions.length !== 1) {
      throw new Error('Choose an organization before using MDSweep.');
    }

    const session = availableSessions[0];

    // Required before the protected POST.
    await firstValueFrom(
      this.#api.http.get<AntiforgeryResponse>(this.#api.url('auth/antiforgery')),
    );

    await firstValueFrom(
      this.#api.http.post<void>(this.#api.url('auth/tenant-context'), {
        tenantId: session.tenantId,
      }),
    );

    // Refresh the token after the authentication cookie gains the tenant claim.
    await firstValueFrom(
      this.#api.http.get<AntiforgeryResponse>(this.#api.url('auth/antiforgery')),
    );

    return session;
  }

  signIn(): void {
    window.location.assign(this.#api.url('auth/login'));
  }

  async signOut(): Promise<void> {
    const antiforgery = await firstValueFrom(
      this.#api.http.get<AntiforgeryResponse>(this.#api.url('auth/antiforgery')),
    );

    const form = this.#document.createElement('form');
    form.method = 'post';
    form.action = this.#api.url('auth/logout');

    const token = this.#document.createElement('input');
    token.type = 'hidden';
    token.name = '__RequestVerificationToken';
    token.value = antiforgery.token;
    form.append(token);

    this.#document.body.append(form);
    form.submit();
  }
}
