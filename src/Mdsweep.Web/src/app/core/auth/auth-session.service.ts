import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../api/api-client';

export type ProviderContext = {
  appUserId: string;
  providerId: string;
  roles: Array<'Administrator' | 'Dispatcher' | 'Driver'>;
};

type Membership = {
  userId: string;
  tenantId: string;
  role: ProviderContext['roles'][number];
};

type AntiforgeryResponse = {
  token: string;
};

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly api = inject(ApiClient);

  async establish(): Promise<ProviderContext> {
    const memberships = await firstValueFrom(this.api.http.get<Membership[]>(this.api.url('auth/me')));

    const contexts = new Map<string, ProviderContext>();

    for (const membership of memberships) {
      const context = contexts.get(membership.tenantId) ?? {
        appUserId: membership.userId,
        providerId: membership.tenantId,
        roles: [],
      };

      context.roles.push(membership.role);
      contexts.set(membership.tenantId, context);
    }

    const availableContexts = [...contexts.values()];

    if (availableContexts.length !== 1) {
      throw new Error('Choose a Provider before using MDSweep.');
    }

    const context = availableContexts[0];

    // Required before the protected POST.
    await firstValueFrom(
      this.api.http.get<AntiforgeryResponse>(this.api.url('auth/antiforgery')),
    );

    await firstValueFrom(
      this.api.http.post<void>(this.api.url('auth/tenant-context'), {
        tenantId: context.providerId,
      }),
    );

    // Refresh the token after the authentication cookie gains the tenant claim.
    await firstValueFrom(
      this.api.http.get<AntiforgeryResponse>(this.api.url('auth/antiforgery')),
    );

    return context;
  }

  signIn(): void {
    window.location.assign(this.api.url('auth/login'));
  }

  async signOut(): Promise<void> {
    await firstValueFrom(this.api.http.post<void>(this.api.url('auth/logout'), {}));
    window.location.assign('/');
  }
}
