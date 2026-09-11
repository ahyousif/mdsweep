import { DOCUMENT } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from './core/auth/auth-session.service';
import { ApplicationError } from './core/errors/application-error';
import InvitationWelcome from './features/users/invitation-welcome';
import { AppShell } from './shell/app-shell';
import { uiText } from './ui-text';

@Component({
  selector: 'app-root',
  imports: [
    AppShell,
    InvitationWelcome,
    HlmButton,
    HlmSpinner,
    ...HlmAlertImports,
    ...HlmCardImports,
  ],
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(AuthSessionService);
  private readonly document = inject(DOCUMENT);
  private readonly router = inject(Router);

  readonly text = uiText;
  readonly managingAccess = signal(false);
  readonly invitationToken = signal(this.readInvitationToken());

  readonly sessionQuery = injectQuery(() => ({
    queryKey: ['auth', 'session'],
    queryFn: () => this.auth.establish(),
    retry: false,
    staleTime: Number.POSITIVE_INFINITY,
  }));

  readonly activeSession = computed(() => {
    const session = this.sessionQuery.data();
    return session === undefined ? null : this.auth.toTenantSession(session);
  });

  readonly isDriverOnly = computed(() => {
    const roles = this.activeSession()?.roles ?? [];

    return (
      roles.includes('Driver') &&
      !roles.some((role) => role === 'Administrator' || role === 'Dispatcher')
    );
  });

  sessionError(): string {
    const error = this.sessionQuery.error();

    if (error instanceof ApplicationError && error.status === 401) {
      return '';
    }

    return error instanceof Error ? error.message : '';
  }

  // TODO: can we use a guard instead?
  invitationAccepted(): void {
    this.invitationToken.set(null);
    void this.router.navigateByUrl('/');
  }

  private readInvitationToken(): string | null {
    const location = this.document.defaultView?.location;
    if (location?.pathname !== '/invitations/accept') return null;
    return new URLSearchParams(location.search).get('token');
  }
}
