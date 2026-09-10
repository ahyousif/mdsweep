import { Component, computed, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarDays,
  lucideChevronLeft,
  lucideChevronRight,
  lucideSearch,
  lucideUpload,
} from '@ng-icons/lucide';
import { HlmButtonImports } from '@spartan-ng/helm/button';
import { HlmInputImports } from '@spartan-ng/helm/input';

@Component({
  selector: 'app-trip-toolbar',
  imports: [FormsModule, NgIcon, ...HlmButtonImports, ...HlmInputImports],
  providers: [
    provideIcons({
      lucideCalendarDays,
      lucideChevronLeft,
      lucideChevronRight,
      lucideSearch,
      lucideUpload,
    }),
  ],
  templateUrl: './trip-toolbar.html',
})
export default class TripToolbar {
  readonly serviceDate = input.required<Date>();
  readonly search = input('');

  readonly previousDayClicked = output<void>();
  readonly nextDayClicked = output<void>();
  readonly todayClicked = output<void>();
  readonly tomorrowClicked = output<void>();
  readonly thisWeekClicked = output<void>();
  readonly searchChange = output<string>();
  readonly importTripsClicked = output<void>();

  readonly formattedDate = computed(() =>
    new Intl.DateTimeFormat('en-US', {
      weekday: 'short',
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(this.serviceDate()),
  );

  onSearchInput(value: string): void {
    this.searchChange.emit(value);
  }

  previousDay(): void {
    this.previousDayClicked.emit();
  }

  nextDay(): void {
    this.nextDayClicked.emit();
  }

  today(): void {
    this.todayClicked.emit();
  }

  tomorrow(): void {
    this.tomorrowClicked.emit();
  }

  thisWeek(): void {
    this.thisWeekClicked.emit();
  }

  importTrips(): void {
    this.importTripsClicked.emit();
  }
}
