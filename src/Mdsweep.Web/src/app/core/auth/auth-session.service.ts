import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom, switchMap } from 'rxjs';

export type ProviderContext = {
  appUserId: string;
  providerId: string;
  role: 'Dispatcher' | 'Driver';
};

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly http = inject(HttpClient);

  async establish(): Promise<ProviderContext> {
    return firstValueFrom(
      this.http.get<ProviderContext[]>('/api/auth/me').pipe(
        switchMap((contexts) => {
          if (contexts.length !== 1) {
            throw new Error('Choose a Provider before using MDSweep.');
          }

          const context = contexts[0];
          return this.http
            .post('/api/auth/provider-context', { providerId: context.providerId })
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
