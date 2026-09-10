import { Component, computed, input } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendarClock, lucideMapPin } from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmCard } from '@spartan-ng/helm/card';

import { Address, Trip } from '../trips-types';

const timeFormatter = new Intl.DateTimeFormat('en-US', {
  hour: 'numeric',
  minute: '2-digit',
  hour12: true,
});

@Component({
  selector: 'app-trip-detail',
  imports: [NgIcon, HlmCard, ...HlmBadgeImports],
  providers: [
    provideIcons({
      lucideCalendarClock,
      lucideMapPin,
    }),
  ],
  templateUrl: './trip-detail.html',
})
export default class TripDetail {
  readonly trip = input<Trip | null>(null);

  readonly passengerName = computed(() => {
    const trip = this.trip();

    if (!trip) {
      return '';
    }

    return `${trip.passengerFirstName} ${trip.passengerLastName}`;
  });

  readonly driveEstimate = computed(() => {
    const trip = this.trip();

    if (!trip?.estimatedTravelMinutes) {
      return 'Not available';
    }

    if (!trip.estimatedDistanceMeters) {
      return `${trip.estimatedTravelMinutes} min`;
    }

    return `${trip.estimatedTravelMinutes} min · ${metersToMiles(trip.estimatedDistanceMeters)} mi`;
  });

  readonly brokerStatusLabel = computed(() => {
    const status = this.trip()?.brokerStatus;

    if (!status) {
      return 'Not supplied';
    }

    return status
      .replaceAll('_', ' ')
      .toLowerCase()
      .replace(/^\w/, (value) => value.toUpperCase());
  });

  formatTime(value: string | null): string {
    if (!value) {
      return 'Not supplied';
    }

    const [hours, minutes] = value.split(':').map(Number);

    return timeFormatter.format(new Date(2000, 0, 1, hours, minutes));
  }

  formatAddress(address: Address): string {
    return [address.city, address.state, address.zip].filter(Boolean).join(', ');
  }
}

function metersToMiles(meters: number): string {
  return (meters / 1609.344).toFixed(1);
}
