import { TranslatePipe } from '@ngx-translate/core';
import { Component, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronRight } from '@ng-icons/lucide';
import { HlmTableImports } from '@spartan-ng/helm/table';

import { type Passenger } from './passengers.api';

@Component({
  selector: 'app-passenger-list',
  imports: [TranslatePipe, NgIcon, ...HlmTableImports],
  providers: [provideIcons({ lucideChevronRight })],
  templateUrl: './passenger-list.html',
})
export default class PassengerList {
  readonly passengers = input.required<Passenger[]>();
  readonly selectedPassengerId = input<string | null>(null);
  readonly passengerSelected = output<Passenger>();

  fullName(passenger: Passenger): string {
    return `${passenger.firstName} ${passenger.lastName}`;
  }

  initials(passenger: Passenger): string {
    return `${passenger.firstName.charAt(0)}${passenger.lastName.charAt(0)}`.toUpperCase();
  }

  formatPhone(value: string | null): string | null {
    if (!value) return null;
    const digits = value.replace(/\D/g, '');
    if (digits.length === 10) return `(${digits.slice(0, 3)}) ${digits.slice(3, 6)}-${digits.slice(6)}`;
    if (digits.length === 11 && digits.startsWith('1')) {
      return `+1 (${digits.slice(1, 4)}) ${digits.slice(4, 7)}-${digits.slice(7)}`;
    }
    return value;
  }

  select(passenger: Passenger): void {
    this.passengerSelected.emit(passenger);
  }
}
