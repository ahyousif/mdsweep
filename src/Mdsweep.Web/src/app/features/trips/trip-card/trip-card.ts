import { Component, computed, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendarClock, lucideChevronRight, lucideMapPin } from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmCard } from '@spartan-ng/helm/card';

import { Trip } from '../trips-types';

const timeFormatter = new Intl.DateTimeFormat('en-US', {
  hour: 'numeric',
  minute: '2-digit',
  hour12: true,
});

@Component({
  selector: 'app-trip-card',
  imports: [NgIcon, HlmCard, ...HlmBadgeImports],
  providers: [
    provideIcons({
      lucideCalendarClock,
      lucideChevronRight,
      lucideMapPin,
    }),
  ],
  host: {
    class: 'block',
  },
  templateUrl: './trip-card.html',
})
export default class TripCard {
  readonly trip = input.required<Trip>();
  readonly selected = input(false);

  readonly tripSelected = output<Trip>();

  readonly passengerName = computed(
    () => `${this.trip().passengerFirstName} ${this.trip().passengerLastName}`,
  );

  readonly pickupTime = computed(() => formatTime(this.trip().scheduledPickupTime, 'Not set'));

  readonly brokerStatusLabel = computed(() => {
    const status = this.trip().brokerStatus;

    if (!status || status.toUpperCase() === 'VALID') {
      return null;
    }

    const label = status
      .replaceAll('_', ' ')
      .toLowerCase()
      .replace(/^\w/, (value) => value.toUpperCase());

    return `Broker: ${label}`;
  });

  readonly timingLabel = computed(() => {
    const trip = this.trip();

    if (trip.direction === 'To') {
      return trip.appointmentTime
        ? `Appt: ${formatTime(trip.appointmentTime)}`
        : 'Appt: Not supplied';
    }

    if (trip.isWillCall) {
      return 'Will call';
    }

    return trip.returnPickupTime
      ? `Return: ${formatTime(trip.returnPickupTime)}`
      : 'Return: Not supplied';
  });

  select(): void {
    this.tripSelected.emit(this.trip());
  }
}

function formatTime(value: string | null, fallback = 'Not supplied'): string {
  if (!value) {
    return fallback;
  }

  const [hours, minutes] = value.split(':').map(Number);

  return timeFormatter.format(new Date(2000, 0, 1, hours, minutes));
}
