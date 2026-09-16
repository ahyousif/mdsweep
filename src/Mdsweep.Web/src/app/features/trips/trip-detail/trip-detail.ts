import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarDays,
  lucideCarFront,
  lucideClock3,
  lucideEllipsisVertical,
  lucideMapPin,
  lucideMove,
  lucidePen,
  lucidePlus,
  lucideTrash2,
  lucideUnlink,
  lucideUserRound,
  lucideX,
} from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';

import { JourneyViewModel, tripAssignmentSummary, tripStatus } from '../journey-view-model';
import { Address, Trip } from '../trips-types';

@Component({
  selector: 'app-trip-detail',
  imports: [
    TranslatePipe,
    NgIcon,
    HlmButton,
    ...HlmBadgeImports,
    ...HlmCardImports,
    ...HlmDropdownMenuImports,
    ...HlmTabsImports,
  ],
  providers: [
    provideIcons({
      lucideCalendarDays,
      lucideCarFront,
      lucideClock3,
      lucideEllipsisVertical,
      lucideMapPin,
      lucideMove,
      lucidePen,
      lucidePlus,
      lucideTrash2,
      lucideUnlink,
      lucideUserRound,
      lucideX,
    }),
  ],
  host: { class: 'block h-full min-h-0' },
  templateUrl: './trip-detail.html',
})
export default class TripDetail {
  readonly language = inject(LanguageService);
  readonly journey = input.required<JourneyViewModel>();
  readonly selectedTripId = input<string | null>(null);

  readonly closed = output<void>();
  readonly tripSelected = output<Trip>();
  readonly changeScheduledPickup = output<Trip>();

  readonly selectedTrip = computed(
    () =>
      this.journey().trips.find((trip) => trip.id === this.selectedTripId()) ??
      this.journey().firstTrip,
  );

  readonly routeStops = computed(() => {
    const route = journeyRoute(this.journey().trips);

    // A reciprocal pair uses the same address as its first and last stop. Show the two
    // distinct endpoints once in the compact card; individual legs remain below.
    if (
      this.journey().trips.length === 2 &&
      route.length === 3 &&
      addressKey(route[0]) === addressKey(route[2])
    ) {
      return route.slice(0, 2);
    }

    return route;
  });

  readonly directionsUrl = computed(() => {
    const route = journeyRoute(this.journey().trips);
    const params = new URLSearchParams({
      api: '1',
      origin: formatFullAddress(route[0]),
      destination: formatFullAddress(route.at(-1)!),
      travelmode: 'driving',
    });

    if (route.length > 2) {
      params.set('waypoints', route.slice(1, -1).map(formatFullAddress).join('|'));
    }

    return `https://www.google.com/maps/dir/?${params}`;
  });

  status(trip: Trip) {
    return tripStatus(trip);
  }

  assignmentSummary(trip: Trip) {
    return tripAssignmentSummary(trip);
  }

  formatTime(value: string | null, fallback = 'common.notSupplied'): string {
    return this.language.formatTime(value, fallback);
  }

  formatAddress(address: Address): string {
    return [address.city, address.state, address.zip].filter(Boolean).join(', ');
  }
}

function journeyRoute(trips: Trip[]): Address[] {
  const route: Address[] = [];

  for (const trip of trips) {
    if (!route.length || addressKey(route.at(-1)!) !== addressKey(trip.pickup)) {
      route.push(trip.pickup);
    }
    if (addressKey(route.at(-1)!) !== addressKey(trip.dropoff)) {
      route.push(trip.dropoff);
    }
  }

  return route;
}

function addressKey(address: Address): string {
  return formatFullAddress(address).toLocaleUpperCase();
}

function formatFullAddress(address: Address): string {
  return [address.address, address.city, address.state, address.zip].filter(Boolean).join(', ');
}
