import { ChangeDetectionStrategy, Component, effect, inject } from '@angular/core';
import { Router, RouterOutlet } from '@angular/router';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from '../core/auth/auth-session.service';
import { uiText } from '../ui-text';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, HlmButton, HlmSpinner, ...HlmAlertImports, ...HlmCardImports],
  templateUrl: './app-shell.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShell {
  private readonly auth = inject(AuthSessionService);
  private readonly router = inject(Router);

  protected readonly text = uiText;
  protected readonly sessionQuery = injectQuery(() => ({
    queryKey: ['auth', 'session'],
    queryFn: () => this.auth.establish(),
    retry: false,
    staleTime: Number.POSITIVE_INFINITY,
  }));

  constructor() {
    effect(() => {
      const context = this.sessionQuery.data();
      if (!context) return;
      if (context.roles.some((role) => role === 'Administrator' || role === 'Dispatcher')) {
        if (this.router.url !== '/trips') {
          void this.router.navigateByUrl('/trips');
        }
      }
    });
  }

  protected signIn(): void {
    this.auth.signIn();
  }

  protected sessionError(): string {
    const error = this.sessionQuery.error();
    return error instanceof Error && !error.message.includes('401') ? error.message : '';
  }

  protected isDriverOnly(): boolean {
    const roles = this.sessionQuery.data()?.roles ?? [];
    return roles.includes('Driver') && !roles.some((role) => role === 'Administrator' || role === 'Dispatcher');
  }
}
