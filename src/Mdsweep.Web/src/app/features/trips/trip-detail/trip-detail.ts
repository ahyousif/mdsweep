import { Component, computed, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarDays,
  lucideCarFront,
  lucideCircleHelp,
  lucideClock3,
  lucideEllipsis,
  lucideMapPin,
  lucidePen,
  lucidePhone,
  lucidePlay,
  lucideUserRound,
  lucideX,
} from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCard } from '@spartan-ng/helm/card';

import { Address, Trip } from '../trips-types';

const timeFormatter = new Intl.DateTimeFormat('en-US', {
  hour: 'numeric',
  minute: '2-digit',
  hour12: true,
});

@Component({
  selector: 'app-trip-detail',
  imports: [NgIcon, HlmButton, HlmCard, ...HlmBadgeImports],
  providers: [
    provideIcons({
      lucideCalendarDays,
      lucideCarFront,
      lucideCircleHelp,
      lucideClock3,
      lucideEllipsis,
      lucideMapPin,
      lucidePen,
      lucidePhone,
      lucidePlay,
      lucideUserRound,
      lucideX,
    }),
  ],
  host: {
    class: 'block h-full min-h-0',
  },
  templateUrl: './trip-detail.html',
})
export default class TripDetail {
  readonly trip = input.required<Trip>();
  readonly closed = output<void>();

  readonly passengerName = computed(
    () => `${this.trip().passengerFirstName} ${this.trip().passengerLastName}`,
  );

  readonly primaryTimeLabel = computed(() =>
    this.trip().direction === 'To' ? 'Appointment' : 'Return pickup',
  );

  readonly primaryTime = computed(() => {
    const trip = this.trip();

    if (trip.direction === 'To') {
      return this.formatTime(trip.appointmentTime);
    }

    if (trip.isWillCall) {
      return 'Will call';
    }

    return this.formatTime(trip.returnPickupTime);
  });

  readonly scheduledPickupSource = computed(() => {
    const trip = this.trip();

    if (trip.manualPickupTime) {
      return 'Using manual time';
    }

    if (trip.calculatedPickupTime) {
      return 'Using calculated time';
    }

    if (trip.returnPickupTime) {
      return 'Using broker time';
    }

    return '';
  });

  readonly driveEstimate = computed(() => {
    const trip = this.trip();

    if (!trip.estimatedTravelMinutes) {
      return 'Not available';
    }

    if (!trip.estimatedDistanceMeters) {
      return `${trip.estimatedTravelMinutes} min`;
    }

    return `${trip.estimatedTravelMinutes} min · ${metersToMiles(trip.estimatedDistanceMeters)} mi`;
  });

  readonly brokerStatusLabel = computed(() => {
    const status = this.trip().brokerStatus;

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
