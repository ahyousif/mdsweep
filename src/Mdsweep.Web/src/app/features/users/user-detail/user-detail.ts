import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideBan,
  lucidePen,
  lucideRotateCcw,
  lucideSend,
  lucideTrash2,
  lucideX,
} from '@ng-icons/lucide';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmSeparator } from '@spartan-ng/helm/separator';

import { UserForm } from '../user-form';
import { type UserDetails, type UserListItem } from '../users.api';

@Component({
  selector: 'app-user-detail',
  imports: [TranslatePipe, NgIcon, HlmBadge, HlmButton, HlmSeparator, UserForm],
  providers: [
    provideIcons({ lucideBan, lucidePen, lucideRotateCcw, lucideSend, lucideTrash2, lucideX }),
  ],
  host: { class: 'block h-full min-h-0' },
  templateUrl: './user-detail.html',
})
export default class UserDetail {
  readonly language = inject(LanguageService);
  readonly user = input.required<UserListItem>();
  readonly editing = input(false);
  readonly busy = input(false);

  readonly closed = output<void>();
  readonly editClicked = output<void>();
  readonly editCancelled = output<void>();
  readonly saved = output<UserDetails>();
  readonly accessChanged = output<boolean>();
  readonly invitationResent = output<void>();
  readonly invitationCancelled = output<void>();

  readonly formValue = computed<UserDetails>(() => {
    const user = this.user();

    return {
      displayName: user.displayName,
      firstName: user.firstName,
      lastName: user.lastName,
      email: user.email,
      roles: user.roles,
      isActive: user.status === 'Active',
    };
  });

  initials(): string {
    return `${this.user().firstName.charAt(0)}${this.user().lastName.charAt(0)}`.toUpperCase();
  }

  statusLabel(): string {
    return this.user().status === 'Inactive' ? 'Disabled' : this.user().status;
  }
}
