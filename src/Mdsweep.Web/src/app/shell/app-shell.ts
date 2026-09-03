import { Component, inject, input } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideMonitor, lucideMoon, lucideRoute, lucideSun } from '@ng-icons/lucide';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmSidebarImports } from '@spartan-ng/helm/sidebar';
import { AuthSessionService, type TenantSession } from '@app/core/auth/auth-session.service';
import { type ThemePreference, ThemeService } from '@app/core/theme/theme.service';

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
  providers: [provideIcons({ lucideMonitor, lucideMoon, lucideRoute, lucideSun })],
  templateUrl: './app-shell.html',
})
export class AppShell {
  private readonly auth = inject(AuthSessionService);
  readonly theme = inject(ThemeService);

  readonly session = input.required<TenantSession>();

  readonly navigation = [
    { label: 'Trips', route: '/trips', icon: 'lucideRoute' },
  ];

  setTheme(theme: ThemePreference): void {
    this.theme.setTheme(theme);
  }

  signOut(): void {
    void this.auth.signOut();
  }
}
