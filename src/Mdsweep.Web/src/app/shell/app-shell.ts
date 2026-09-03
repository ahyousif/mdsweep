import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { ApplicationError } from '@app/core/errors/application-error';
import { uiText } from '@app/ui-text';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HlmButton, HlmSpinner, ...HlmAlertImports, ...HlmCardImports],
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly auth = inject(AuthSessionService);

  readonly text = uiText;

  readonly sessionQuery = injectQuery(() => ({
    queryKey: ['auth', 'session'],
    queryFn: () => this.auth.establish(),
    retry: false,
    staleTime: Number.POSITIVE_INFINITY,
  }));

  signIn(): void {
    this.auth.signIn();
  }

  sessionError(): string {
    const error = this.sessionQuery.error();

    if (error instanceof ApplicationError && error.status === 401) {
      return '';
    }

    return error instanceof Error ? error.message : '';
  }

  isDriverOnly(): boolean {
    const roles = this.sessionQuery.data()?.roles ?? [];

    return (
      roles.includes('Driver') &&
      !roles.some((role) => role === 'Administrator' || role === 'Dispatcher')
    );
  }
}
