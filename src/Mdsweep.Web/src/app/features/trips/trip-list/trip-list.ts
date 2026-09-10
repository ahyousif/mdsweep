import { Component, computed, input, output } from '@angular/core';

import TripCard from '../trip-card/trip-card';
import { Trip } from '../trips-types';

type TripGroup = {
  label: string;
  trips: Trip[];
};

@Component({
  selector: 'app-trip-list',
  imports: [TripCard],
  templateUrl: './trip-list.html',
})
export default class TripList {
  readonly trips = input.required<Trip[]>();
  readonly selectedTripId = input<string | null>(null);

  readonly tripSelected = output<Trip>();

  readonly groups = computed<TripGroup[]>(() => {
    const trips = this.trips();

    const scheduled = trips
      .filter((trip) => trip.scheduledPickupTime)
      .sort((a, b) => a.scheduledPickupTime!.localeCompare(b.scheduledPickupTime!));

    const unscheduled = trips.filter((trip) => !trip.scheduledPickupTime);

    return [
      {
        label: 'Morning',
        trips: scheduled.filter((trip) => getHour(trip.scheduledPickupTime!) < 12),
      },
      {
        label: 'Afternoon',
        trips: scheduled.filter((trip) => {
          const hour = getHour(trip.scheduledPickupTime!);
          return hour >= 12 && hour < 17;
        }),
      },
      {
        label: 'Evening',
        trips: scheduled.filter((trip) => getHour(trip.scheduledPickupTime!) >= 17),
      },
      {
        label: 'Unscheduled',
        trips: unscheduled,
      },
    ].filter((group) => group.trips.length > 0);
  });

  selectTrip(trip: Trip): void {
    this.tripSelected.emit(trip);
  }
}

function getHour(time: string): number {
  return Number(time.split(':')[0]);
}
