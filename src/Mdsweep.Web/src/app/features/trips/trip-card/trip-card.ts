import { LanguageService } from '@app/core/i18n/language.service';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideChevronRight,
  lucideMapPin,
  lucideMoreHorizontal,
  lucidePlus,
} from '@ng-icons/lucide';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDropdownMenuImports } from '@spartan-ng/helm/dropdown-menu';

import { JourneyViewModel } from '../journey-view-model';

@Component({
  selector: 'app-trip-card',
  imports: [TranslatePipe, NgIcon, HlmButton, ...HlmBadgeImports, ...HlmDropdownMenuImports],
  providers: [
    provideIcons({
      lucideChevronRight,
      lucideMapPin,
      lucideMoreHorizontal,
      lucidePlus,
    }),
  ],
  host: { class: 'block' },
  templateUrl: './trip-card.html',
})
export default class TripCard {
  readonly language = inject(LanguageService);
  readonly journey = input.required<JourneyViewModel>();
  readonly selected = input(false);

  readonly journeySelected = output<JourneyViewModel>();

  readonly pickupTime = computed(() => {
    const journey = this.journey();

    if (!journey.pickupTime && journey.isWillCall) {
      return null;
    }

    return this.language.formatTime(journey.pickupTime, 'common.notSet');
  });

  select(): void {
    this.journeySelected.emit(this.journey());
  }

  stopRowSelection(event: Event): void {
    event.stopPropagation();
  }
}
