import { TranslatePipe } from '@ngx-translate/core';
import { Component, input, output } from '@angular/core';

import TripCard from '../trip-card/trip-card';
import { JourneyViewModel } from '../journey-view-model';

@Component({
  selector: 'app-trip-list',
  imports: [TranslatePipe, TripCard],
  templateUrl: './trip-list.html',
})
export default class TripList {
  readonly journeys = input.required<JourneyViewModel[]>();
  readonly selectedJourneyId = input<string | null>(null);

  readonly journeySelected = output<JourneyViewModel>();
}
