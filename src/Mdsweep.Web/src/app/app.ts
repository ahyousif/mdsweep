import { Component, computed, inject } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from './core/auth/auth-session.service';
import { ApplicationError } from './core/errors/application-error';
import { AppShell } from './shell/app-shell';
import { uiText } from './ui-text';

@Component({
  selector: 'app-root',
  imports: [AppShell, HlmButton, HlmSpinner, ...HlmAlertImports, ...HlmCardImports],
  templateUrl: './app.html',
})
export class App {
  private readonly auth = inject(AuthSessionService);

  readonly text = uiText;

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

  async selectTenant(tenantId: string): Promise<void> {
    await this.auth.selectTenant(tenantId);
    await this.sessionQuery.refetch();
  }

  sessionError(): string {
    const error = this.sessionQuery.error();

    if (error instanceof ApplicationError && error.status === 401) {
      return '';
    }

    return error instanceof Error ? error.message : '';
  }
}
