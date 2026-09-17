import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideEllipsisVertical, lucideMapPin, lucidePlus } from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';
import { hlm } from '@spartan-ng/helm/utils';

import { JOURNEY_ROW_LAYOUT } from '../journey-row-layout';
import { JourneyViewModel } from '../journey-view-model';

@Component({
  selector: 'app-trip-card',
  imports: [TranslatePipe, NgIcon, HlmButton, ...HlmBadgeImports, ...HlmDropdownMenuImports],
  providers: [
    provideIcons({
      lucideEllipsisVertical,
      lucideMapPin,
      lucidePlus,
    }),
  ],
  host: { class: 'block' },
  templateUrl: './trip-card.html',
})
export default class TripCard {
  readonly rowClasses = hlm(
    JOURNEY_ROW_LAYOUT,
    'relative grid rounded-lg border py-3 transition-colors',
  );
  readonly language = inject(LanguageService);
  readonly journey = input.required<JourneyViewModel>();
  readonly selected = input(false);

  readonly journeySelected = output<JourneyViewModel>();

  readonly pickupTime = computed(() => {
    const journey = this.journey();

    return this.language.formatTime(
      journey.displayPickupTime,
      journey.isEntirelyWillCall ? 'trips.willCall' : 'common.notSet',
    );
  });

  select(): void {
    this.journeySelected.emit(this.journey());
  }
}
