import { Component, inject, input, output, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { AuthSessionService, type TenantSession } from '@app/core/auth/auth-session.service';
import { type ThemePreference, ThemeService } from '@app/core/theme/theme.service';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideMonitor, lucideMoon, lucideRoute, lucideSun, lucideUsers } from '@ng-icons/lucide';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmSidebarImports } from '@spartan-ng/helm/sidebar';

@Component({
  selector: 'app-shell',
  imports: [
    RouterLink,
    RouterLinkActive,
    RouterOutlet,
    NgIcon,
    ...HlmDropdownMenuImports,
    ...HlmSidebarImports,
  ],
  providers: [provideIcons({ lucideMonitor, lucideMoon, lucideRoute, lucideSun, lucideUsers })],
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly auth = inject(AuthSessionService);
  readonly theme = inject(ThemeService);

  readonly session = input.required<TenantSession>();
  readonly manageAccess = output();
  readonly signOutPending = signal(false);
  readonly signOutError = signal('');

  readonly navigation = [
    { label: 'Trips', route: '/trips', icon: 'lucideRoute' },
    { label: 'Users', route: '/users', icon: 'lucideUsers' },
  ];

  setTheme(theme: ThemePreference): void {
    this.theme.setTheme(theme);
  }

  async signOut(): Promise<void> {
    if (this.signOutPending()) {
      return;
    }

    this.signOutError.set('');
    this.signOutPending.set(true);

    try {
      await this.auth.signOut();

      // On success the browser is navigating away through the OIDC
      // logout flow, so leave the action pending.
    } catch (error) {
      this.signOutPending.set(false);
      this.signOutError.set(httpErrorMessage(error, 'Could not sign out. Try again.'));
    }
  }
}
