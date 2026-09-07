import { DatePipe } from '@angular/common';
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
import { userQueryKeys } from './users.queries';

@Component({
  selector: 'app-invitation-welcome',
  imports: [DatePipe, HlmButton, HlmSpinner, HlmMuted, HlmAlertImports, HlmCardImports],
  templateUrl: './invitation-welcome.html',
})
export class InvitationWelcome {
  readonly #api = inject(UsersApi);
  readonly #auth = inject(AuthSessionService);
  readonly #queries = inject(QueryClient);
  readonly hasAccess = input(false);
  readonly closed = output();
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
  readonly invitation = injectQuery(() => ({
    queryKey: userQueryKeys.invitation,
    queryFn: () => this.#api.pendingInvitation(),
    retry: false,
  }));
  readonly acceptance = injectMutation(() => ({
    mutationFn: (id: string) => this.#api.accept(id),
    onSuccess: async () => {
      await Promise.all([
        this.#queries.invalidateQueries({ queryKey: ['auth'] }),
        this.#queries.invalidateQueries({ queryKey: userQueryKeys.invitation }),
      ]);
    },
    onError: (error: unknown) =>
      this.error.set(httpErrorMessage(error, 'The invitation could not be accepted. Try again.')),
  }));
  invitationError(): string {
    return httpErrorMessage(
      this.invitation.error(),
      'The invitations could not be checked. Try again.',
    );
  }
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
