import { Component, inject, input, output, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmMuted } from '@spartan-ng/helm/typography';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { injectMutation, injectQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { UsersApi } from './users.api';

@Component({
  selector: 'app-invitation-welcome',
  imports: [HlmButton, HlmSpinner, HlmMuted, HlmAlertImports, HlmCardImports],
  templateUrl: './invitation-welcome.html',
})
export class InvitationWelcome {
  readonly #api = inject(UsersApi);
  readonly #auth = inject(AuthSessionService);
  readonly #queries = inject(QueryClient);
  readonly hasAccess = input(false);
  readonly token = input<string | null>(null);
  readonly closed = output();
  readonly accepted = output();
  readonly error = signal('');
  readonly sessions = injectQuery(() => ({
    queryKey: ['auth', 'memberships'],
    queryFn: () => this.#auth.availableSessions(),
    retry: false,
  }));
  readonly selection = injectMutation(() => ({
    mutationFn: (tenantId: string) => this.#auth.switchTenant(tenantId),
    onError: (error: unknown) =>
      this.error.set(httpErrorMessage(error, 'Could not switch Tenant. Try again.')),
  }));
  readonly signingOut = signal(false);
  readonly acceptance = injectMutation(() => ({
    mutationFn: (token: string) => this.#api.accept(token),
    onSuccess: async () => {
      await this.#queries.invalidateQueries({ queryKey: ['auth'] });
      this.accepted.emit();
    },
    onError: (error: unknown) =>
      this.error.set(httpErrorMessage(error, 'The invitation could not be accepted. Try again.')),
  }));
  async signOut(): Promise<void> {
    this.signingOut.set(true);
    try {
      await this.#auth.signOut();
    } catch (error) {
      this.error.set(httpErrorMessage(error, 'Could not sign out. Try again.'));
      this.signingOut.set(false);
    }
  }
}
