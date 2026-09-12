import { LocalizedCalendar } from '@app/core/i18n/localized-calendar';
import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronLeft,
  lucideChevronRight,
  lucideSearch,
  lucideUpload,
} from '@ng-icons/lucide';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmInput } from '@spartan-ng/helm/input';

@Component({
  selector: 'app-trip-toolbar',
  imports: [LocalizedCalendar, TranslatePipe, NgIcon, HlmButton, HlmInput, ...HlmDatePickerImports],
  providers: [
    provideIcons({
      lucideChevronLeft,
      lucideChevronRight,
      lucideSearch,
      lucideUpload,
    }),
  ],
  templateUrl: './trip-toolbar.html',
})
export default class TripToolbar {
  readonly language = inject(LanguageService);
  readonly serviceDate = input.required<Date>();
  readonly search = input('');

  readonly serviceDateChange = output<Date>();
  readonly previousDayClicked = output<void>();
  readonly nextDayClicked = output<void>();
  readonly todayClicked = output<void>();
  readonly tomorrowClicked = output<void>();
  readonly thisWeekClicked = output<void>();
  readonly searchChange = output<string>();
  readonly importTripsClicked = output<void>();

  readonly formatServiceDate = computed(() => {
    const locale = this.language.locale();
    const formatter = new Intl.DateTimeFormat(locale, {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    });
    return (date: Date) => formatter.format(date);
  });

  readonly isToday = computed(() => isSameDay(this.serviceDate(), new Date()));

  readonly isTomorrow = computed(() => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);

    return isSameDay(this.serviceDate(), tomorrow);
  });

  onServiceDateChange(date: Date | null): void {
    if (date) {
      this.serviceDateChange.emit(date);
    }
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;

    this.searchChange.emit(input.value);
  }
}

function isSameDay(a: Date, b: Date): boolean {
  return (
    a.getFullYear() === b.getFullYear() &&
    a.getMonth() === b.getMonth() &&
    a.getDate() === b.getDate()
  );
}
