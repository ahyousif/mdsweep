import { LanguageService } from '@app/core/i18n/language.service';
import { UiMessagePipe, type UiFeedback } from '@app/core/i18n/ui-message';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideArrowLeft, lucidePen, lucideX } from '@ng-icons/lucide';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmSeparator } from '@spartan-ng/helm/separator';

import { type Passenger } from './passengers.api';

@Component({
  selector: 'app-passenger-detail',
  imports: [TranslatePipe, UiMessagePipe, NgIcon, HlmBadge, HlmButton, HlmSeparator, ...HlmAlertImports],
  providers: [provideIcons({ lucideArrowLeft, lucidePen, lucideX })],
  templateUrl: './passenger-detail.html',
})
export default class PassengerDetail {
  readonly language = inject(LanguageService);
  readonly passenger = input.required<Passenger>();
  readonly busy = input(false);
  readonly error = input<UiFeedback | null>(null);
  readonly closed = output<void>();
  readonly editClicked = output<void>();
  readonly disableClicked = output<void>();
  readonly enableClicked = output<void>();

  fullName(): string {
    const passenger = this.passenger();
    return `${passenger.firstName} ${passenger.lastName}`;
  }

  initials(): string {
    const passenger = this.passenger();
    return `${passenger.firstName.charAt(0)}${passenger.lastName.charAt(0)}`.toUpperCase();
  }

  formatPhone(value: string | null): string | null {
    if (!value) return null;
    const digits = value.replace(/\D/g, '');
    if (digits.length === 10) return `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
    if (digits.length === 11 && digits.startsWith('1')) return `+1 (${digits.slice(1, 4)}) ${digits.slice(4, 7)}-${digits.slice(7)}`;
    return value;
  }

  dateOfBirth(): Date | null {
    const value = this.passenger().dateOfBirth;
    if (!value) return null;
    const [year, month, day] = value.split('-').map(Number);
    return year && month && day ? new Date(year, month - 1, day) : null;
  }
}
