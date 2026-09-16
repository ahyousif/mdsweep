import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { UiMessagePipe } from '@app/core/i18n/ui-message';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { HlmButton } from '@spartan-ng/helm/button';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';

import { HlmDialogService } from '@spartan-ng/helm/dialog';
import { injectQuery } from '@tanstack/angular-query-experimental';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';

import ScheduledPickupDialog from './scheduled-pickup/scheduled-pickup-dialog';
import TripDetail from './trip-detail/trip-detail';
import TripImportDialog from './trip-import/trip-import-dialog';
import TripList from './trip-list/trip-list';
import TripToolbar from './trip-toolbar/trip-toolbar';
import {
  buildJourneys,
  JourneyFilter,
  JourneyViewModel,
  matchesJourneyFilter,
} from './journey-view-model';
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
  readonly #translate = inject(TranslateService);

  readonly currentDate = signal(new Date());
  readonly weekSelected = signal(false);
  readonly search = signal('');
  readonly selectedFilter = signal<JourneyFilter>('all');
  readonly selectedJourneyId = signal<string | null>(null);
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
    const startDate = this.weekSelected() ? startOfWeek(this.currentDate()) : this.currentDate();
    const endDate = this.weekSelected() ? addDays(startDate, 6) : startDate;

    return {
      startDate: toServiceDate(startDate),
      endDate: toServiceDate(endDate),
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
  readonly journeys = computed(() => buildJourneys(this.trips()));
  readonly displayedJourneys = computed(() =>
    this.journeys().filter((journey) => matchesJourneyFilter(journey, this.selectedFilter())),
  );

  readonly filterCounts = computed(() => ({
    all: this.journeys().length,
    scheduled: this.journeys().filter((journey) => matchesJourneyFilter(journey, 'scheduled'))
      .length,
    needsAttention: this.journeys().filter((journey) =>
      matchesJourneyFilter(journey, 'needsAttention'),
    ).length,
    willCall: this.journeys().filter((journey) => matchesJourneyFilter(journey, 'willCall')).length,
  }));

  readonly selectedJourney = computed<JourneyViewModel | null>(() => {
    const id = this.selectedJourneyId();
    return id ? (this.journeys().find((journey) => journey.id === id) ?? null) : null;
  });

  readonly selectedTrip = computed<Trip | null>(() => {
    const id = this.selectedTripId();

    if (!id) {
      return null;
    }

    return this.selectedJourney()?.trips.find((trip) => trip.id === id) ?? null;
  });

  selectJourney(journey: JourneyViewModel): void {
    this.selectedJourneyId.set(journey.id);
    this.selectedTripId.set(journey.firstTrip.id);
  }

  selectTrip(trip: Trip): void {
    this.selectedTripId.set(trip.id);
  }

  closeTripDetail(): void {
    this.selectedJourneyId.set(null);
    this.selectedTripId.set(null);
  }

  setSearch(value: string): void {
    this.search.set(value);
  }

  setFilter(filter: JourneyFilter): void {
    this.selectedFilter.set(filter);
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
    this.currentDate.set(startOfWeek(new Date()));
    this.weekSelected.set(true);
    this.closeTripDetail();
  }

  setServiceDate(date: Date): void {
    this.#setDate(date);
  }

  openScheduledPickupDialog(trip: Trip): void {
    this.#dialog.open(ScheduledPickupDialog, {
      context: { trip },
      contentClass: 'w-[calc(100vw-2rem)] rounded-lg p-5 sm:w-[29rem] sm:max-w-[29rem]',
      showCloseButton: false,
      disableClose: true,
    });
  }

  openImportDialog(): void {
    this.#dialog.open(TripImportDialog, {
      contentClass: 'w-[calc(100vw-2rem)] sm:w-[36rem] sm:max-w-[36rem]',
      showCloseButton: true,
      closeLabel: this.#translate.instant('common.close'),
    });
  }

  #setDate(date: Date): void {
    this.currentDate.set(date);
    this.weekSelected.set(false);
    this.closeTripDetail();
  }
}

function startOfWeek(date: Date): Date {
  const result = new Date(date);
  const day = result.getDay();
  result.setDate(result.getDate() - (day === 0 ? 6 : day - 1));
  return result;
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
