import type { Trip } from './trips-types';

export function tripReferenceTime(
  trip: Pick<Trip, 'direction' | 'appointmentTime' | 'returnPickupTime' | 'isWillCall'>,
) {
  return {
    labelKey: trip.direction === 'To' ? 'trips.appointment' : 'trips.returnPickup',
    value:
      trip.direction === 'To'
        ? trip.appointmentTime
        : trip.isWillCall
          ? null
          : trip.returnPickupTime,
    fallbackKey:
      trip.direction === 'From' && trip.isWillCall ? 'trips.willCall' : 'common.notSupplied',
  };
}
