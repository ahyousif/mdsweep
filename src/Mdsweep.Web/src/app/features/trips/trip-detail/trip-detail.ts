import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarDays,
  lucideCarFront,
  lucideCircleHelp,
  lucideClock3,
  lucideMapPin,
  lucidePen,
  lucidePhone,
  lucideUserRound,
  lucideX,
} from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCard } from '@spartan-ng/helm/card';

import { Address, Trip } from '../trips-types';

@Component({
  selector: 'app-trip-detail',
  imports: [TranslatePipe, NgIcon, HlmButton, HlmCard, ...HlmBadgeImports],
  providers: [
    provideIcons({
      lucideCalendarDays,
      lucideCarFront,
      lucideCircleHelp,
      lucideClock3,
      lucideMapPin,
      lucidePen,
      lucidePhone,
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
  readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  readonly trip = input.required<Trip>();
  readonly closed = output<void>();
  readonly changeScheduledPickup = output<void>();

  readonly passengerName = computed(
    () => `${this.trip().passengerFirstName} ${this.trip().passengerLastName}`,
  );

  readonly primaryTimeLabel = computed(() => {
    this.language.language();
    return this.translate.instant(
      this.trip().direction === 'To' ? 'trips.appointment' : 'trips.returnPickup',
    );
  });

  readonly primaryTime = computed(() => {
    this.language.language();
    const trip = this.trip();

    if (trip.direction === 'To') {
      return this.formatTime(trip.appointmentTime);
    }

    if (trip.isWillCall) {
      return this.translate.instant('trips.willCall');
    }

    return this.formatTime(trip.returnPickupTime);
  });

  readonly scheduledPickupSource = computed(() => {
    const trip = this.trip();

    this.language.language();

    if (trip.manualPickupTime) {
      return this.translate.instant('trips.manualTime');
    }

    if (trip.calculatedPickupTime) {
      return this.translate.instant('trips.calculatedTime');
    }

    if (trip.returnPickupTime) {
      return this.translate.instant('trips.brokerTime');
    }

    return trip.isWillCall ? this.translate.instant('trips.willCall') : '';
  });

  readonly driveEstimate = computed(() => {
    this.language.language();
    const trip = this.trip();

    if (trip.estimatedTravelMinutes === null) {
      return this.translate.instant('common.notAvailable');
    }

    if (trip.estimatedDistanceMeters === null) {
      return this.translate.instant('trips.driveMinutes', { minutes: trip.estimatedTravelMinutes });
    }

    return this.translate.instant('trips.driveDistance', {
      minutes: trip.estimatedTravelMinutes,
      miles: metersToMiles(trip.estimatedDistanceMeters),
    });
  });

  readonly brokerStatusLabel = computed(() => {
    this.language.language();
    const status = this.trip().brokerStatus;

    if (!status) {
      return this.translate.instant('common.notSupplied');
    }

    return status.toUpperCase() === 'VALID' ? this.translate.instant('trips.brokerValid') : status;
  });

  readonly directionsUrl = computed(() => {
    const trip = this.trip();
    const address = (value: Address) =>
      [value.address, value.city, value.state, value.zip].filter(Boolean).join(', ');
    const params = new URLSearchParams({
      api: '1',
      origin: address(trip.pickup),
      destination: address(trip.dropoff),
      travelmode: 'driving',
    });
    return `https://www.google.com/maps/dir/?${params}`;
  });

  formatTime(value: string | null): string {
    return this.language.formatTime(value);
  }

  formatAddress(address: Address): string {
    return [address.city, address.state, address.zip].filter(Boolean).join(', ');
  }
}

function metersToMiles(meters: number): string {
  return (meters / 1609.344).toFixed(1);
}
