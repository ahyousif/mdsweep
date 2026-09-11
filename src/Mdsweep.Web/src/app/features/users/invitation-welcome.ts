import { DOCUMENT } from '@angular/common';
import { Component, inject, input, output, signal } from '@angular/core';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { ApplicationError } from '@app/core/errors/application-error';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmMuted } from '@spartan-ng/helm/typography';
import { injectMutation, injectQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { UsersApi } from './users.api';

@Component({
  selector: 'app-invitation-welcome',
  imports: [HlmButton, HlmSpinner, HlmMuted, HlmAlertImports, HlmCardImports],
  templateUrl: './invitation-welcome.html',
})
export default class InvitationWelcome {
  readonly #api = inject(UsersApi);
  readonly #auth = inject(AuthSessionService);
  readonly #document = inject(DOCUMENT);
  readonly #queries = inject(QueryClient);
  readonly hasAccess = input(false);
  readonly token = input<string | null>(null);
  readonly currentEmail = input<string | null>(null);
  readonly closed = output();
  readonly accepted = output();
  readonly error = signal('');
  readonly accountMismatch = signal(false);
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
      await this.#queries.invalidateQueries({ queryKey: ['auth', 'session'], refetchType: 'none' });
      const session = await this.#queries.fetchQuery({
        queryKey: ['auth', 'session'],
        queryFn: () => this.#auth.establish(),
        staleTime: 0,
      });
      await this.#queries.invalidateQueries({ queryKey: ['auth', 'memberships'] });

      if (this.#auth.toTenantSession(session) === null) {
        this.error.set(
          'Tenant access could not be established. Try accepting the invitation again.',
        );
        return;
      }

      this.accepted.emit();
    },
    onError: (error: unknown) => {
      if (
        error instanceof ApplicationError &&
        error.validationErrors['invitationEmailMismatch'] !== undefined
      ) {
        this.accountMismatch.set(true);
        this.error.set('');
        return;
      }
      this.error.set(httpErrorMessage(error, 'The invitation could not be accepted. Try again.'));
    },
  }));

  continueWithAnotherAccount(): void {
    this.signingOut.set(true);
    const location = this.#document.defaultView?.location;
    const returnUrl = location
      ? `${location.pathname}${location.search}${location.hash}`
      : '/invitations/accept';
    this.#auth.signOut(returnUrl);
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
