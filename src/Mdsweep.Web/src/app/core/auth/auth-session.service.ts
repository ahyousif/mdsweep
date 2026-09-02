import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom, switchMap } from 'rxjs';

export type ProviderContext = {
  appUserId: string;
  providerId: string;
  roles: Array<'Administrator' | 'Dispatcher' | 'Driver'>;
};

type Membership = { userId: string; tenantId: string; role: ProviderContext['roles'][number] };

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly http = inject(HttpClient);

  async establish(): Promise<ProviderContext> {
    return firstValueFrom(
      this.http.get<Membership[]>('/api/auth/me').pipe(
        switchMap((memberships: Membership[]) => {
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
          return this.http
            .post('/api/auth/tenant-context', { tenantId: context.providerId })
            .pipe(
              switchMap(() => this.http.get('/api/auth/antiforgery')),
              switchMap(() => [context]),
            );
        }),
      ),
    );
  }

  signIn(): void {
    window.location.assign('/api/auth/login');
  }
}
