import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCalendarClock, lucideChevronRight, lucideMapPin } from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmCard } from '@spartan-ng/helm/card';

import { Trip } from '../trips-types';

@Component({
  selector: 'app-trip-card',
  imports: [TranslatePipe, NgIcon, HlmCard, ...HlmBadgeImports],
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
  readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  readonly trip = input.required<Trip>();
  readonly selected = input(false);

  readonly tripSelected = output<Trip>();

  readonly passengerName = computed(
    () => `${this.trip().passengerFirstName} ${this.trip().passengerLastName}`,
  );

  readonly pickupTime = computed(() =>
    this.language.formatTime(this.trip().scheduledPickupTime, 'common.notSet'),
  );

  readonly brokerStatusLabel = computed(() => {
    const status = this.trip().brokerStatus;

    if (!status || status.toUpperCase() === 'VALID') {
      return null;
    }

    return this.translate.instant('trips.brokerLabel', { status });
  });

  readonly timingLabel = computed(() => {
    const trip = this.trip();

    if (trip.direction === 'To') {
      return this.translate.instant('trips.appointmentTime', {
        time: this.language.formatTime(trip.appointmentTime),
      });
    }

    if (trip.isWillCall) {
      return this.translate.instant('trips.willCall');
    }

    return this.translate.instant('trips.returnTime', {
      time: this.language.formatTime(trip.returnPickupTime),
    });
  });

  select(): void {
    this.tripSelected.emit(this.trip());
  }
}
