import { TranslatePipe } from '@ngx-translate/core';
import { UiMessagePipe } from '@app/core/i18n/ui-message';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { HlmButton } from '@spartan-ng/helm/button';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { HlmDialogService } from '@spartan-ng/helm/dialog';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';

import TripDetail from './trip-detail/trip-detail';
import TripImportDialog from './trip-import/trip-import-dialog';
import TripList from './trip-list/trip-list';
import TripToolbar from './trip-toolbar/trip-toolbar';
import { Trip, TripsQuery } from './trips-types';
import { TripsApi } from './trips.api';
import { tripsQueryOptions } from './trips.queries';

@Component({
  selector: 'app-trips-page',
  imports: [TranslatePipe, UiMessagePipe, HlmButton, TripToolbar, TripList, TripDetail],
  templateUrl: './trips-page.html',
  host: {
    class: 'block h-full min-h-0',
  },
})
export default class TripsPage {
  readonly #api = inject(TripsApi);
  readonly #dialog = inject(HlmDialogService);

  readonly currentDate = signal(new Date());
  readonly search = signal('');
  readonly selectedTripId = signal<string | null>(null);

  readonly debouncedSearch = toSignal(
    toObservable(this.search).pipe(
      map((value) => value.trim()),
      debounceTime(300),
      distinctUntilChanged(),
    ),
    {
      initialValue: '',
    },
  );

  readonly query = computed<TripsQuery>(() => {
    const serviceDate = toServiceDate(this.currentDate());

    return {
      startDate: serviceDate,
      endDate: serviceDate,
      search: this.debouncedSearch() || undefined,
      page: 1,
      pageSize: 100,
    };
  });

  readonly tripsQuery = injectQuery(() => tripsQueryOptions(this.#api, this.query()));

  readonly loadError = computed(() =>
    httpErrorMessage(this.tripsQuery.error(), 'errors.loadTrips'),
  );

  readonly trips = computed(() => this.tripsQuery.data()?.items ?? []);

  readonly selectedTrip = computed<Trip | null>(() => {
    const id = this.selectedTripId();

    if (!id) {
      return null;
    }

    return this.trips().find((trip) => trip.id === id) ?? null;
  });

  selectTrip(trip: Trip): void {
    this.selectedTripId.set(trip.id);
  }

  closeTripDetail(): void {
    this.selectedTripId.set(null);
  }

  setSearch(value: string): void {
    this.search.set(value);
  }

  previousDay(): void {
    this.#setDate(addDays(this.currentDate(), -1));
  }

  nextDay(): void {
    this.#setDate(addDays(this.currentDate(), 1));
  }

  setToday(): void {
    this.#setDate(new Date());
  }

  setTomorrow(): void {
    this.#setDate(addDays(new Date(), 1));
  }

  setThisWeek(): void {
    // Multi-day grouping comes later.
  }

  setServiceDate(date: Date): void {
    this.#setDate(date);
  }

  openImportDialog(): void {
    this.#dialog.open(TripImportDialog, {
      contentClass: 'w-[calc(100vw-2rem)] sm:w-[36rem] sm:max-w-[36rem]',
      showCloseButton: true,
    });
  }

  #setDate(date: Date): void {
    this.currentDate.set(date);
    this.selectedTripId.set(null);
  }
}

function addDays(date: Date, amount: number): Date {
  const next = new Date(date);

  next.setDate(next.getDate() + amount);

  return next;
}

function toServiceDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}
