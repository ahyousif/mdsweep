import { TranslatePipe } from '@ngx-translate/core';
import { Component, input, output } from '@angular/core';
import { hlm } from '@spartan-ng/helm/utils';

import { JOURNEY_ROW_LAYOUT } from '../journey-row-layout';
import TripCard from '../trip-card/trip-card';
import { JourneyViewModel } from '../journey-view-model';

@Component({
  selector: 'app-trip-list',
  imports: [TranslatePipe, TripCard],
  templateUrl: './trip-list.html',
})
export default class TripList {
  readonly headerClasses = hlm(
    JOURNEY_ROW_LAYOUT,
    'type-meta hidden border border-transparent @4xl:grid',
  );
  readonly journeys = input.required<JourneyViewModel[]>();
  readonly selectedJourneyId = input<string | null>(null);

  readonly journeySelected = output<JourneyViewModel>();
}
