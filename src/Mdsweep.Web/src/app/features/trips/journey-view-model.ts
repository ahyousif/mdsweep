import { tripReferenceTime } from './trip-reference-time';
import { Address, Trip } from './trips-types';

export type JourneyFilter = 'all' | 'scheduled' | 'needsAttention' | 'willCall';

export type JourneyStatus = {
  key: string;
  params?: Record<string, string>;
  variant: 'default' | 'secondary' | 'destructive' | 'outline';
};

export type JourneyViewModel = {
  id: string;
  passengerName: string;
  memberId: string | null;
  trips: Trip[];
  firstTrip: Trip;
  pickupTime: string | null;
  appointmentTime: string | null;
  isWillCall: boolean;
  origin: Address;
  destination: Address;
  routeContinuation: string;
  routeSummary: string;
  status: JourneyStatus;
  sortTime: string;
};

export function buildJourneys(trips: Trip[]): JourneyViewModel[] {
  const byJourney = new Map<string, Trip[]>();

  for (const trip of trips) {
    const journeyTrips = byJourney.get(trip.journeyId) ?? [];
    journeyTrips.push(trip);
    byJourney.set(trip.journeyId, journeyTrips);
  }

  return Array.from(byJourney, ([id, journeyTrips]) => toJourney(id, journeyTrips)).sort(
    compareJourneys,
  );
}

export function matchesJourneyFilter(journey: JourneyViewModel, filter: JourneyFilter): boolean {
  switch (filter) {
    case 'all':
      return true;
    case 'scheduled':
      return journey.status.key === 'trips.status.scheduled';
    case 'needsAttention':
      return (
        journey.status.key === 'trips.status.needsPickupTime' ||
        journey.status.key === 'trips.status.brokerIssue'
      );
    case 'willCall':
      return journey.isWillCall;
  }
}

export function tripActionTime(trip: Trip): string | null {
  return trip.scheduledPickupTime ?? tripReferenceTime(trip).value;
}

export function tripStatus(trip: Trip): JourneyStatus {
  if (trip.brokerStatus && trip.brokerStatus.toUpperCase() !== 'VALID') {
    return {
      key: 'trips.status.brokerIssue',
      params: { status: trip.brokerStatus },
      variant: 'destructive',
    };
  }

  if (trip.isWillCall && !trip.scheduledPickupTime) {
    return { key: 'trips.willCall', variant: 'secondary' };
  }

  if (!trip.scheduledPickupTime) {
    return { key: 'trips.status.needsPickupTime', variant: 'destructive' };
  }

  return { key: 'trips.status.scheduled', variant: 'default' };
}

function toJourney(id: string, trips: Trip[]): JourneyViewModel {
  const sortedTrips = [...trips].sort(compareTrips);
  const firstTrip = sortedTrips[0];
  const route = routeAddresses(sortedTrips);
  const pickupTime = sortedTrips.map(tripActionTime).find((time) => time !== null) ?? null;
  const appointmentTime = sortedTrips.map((trip) => trip.appointmentTime).find(Boolean) ?? null;
  const isWillCall = sortedTrips.some((trip) => trip.isWillCall && !trip.scheduledPickupTime);

  return {
    id,
    passengerName: `${firstTrip.passengerFirstName} ${firstTrip.passengerLastName}`,
    memberId: firstTrip.memberId,
    trips: sortedTrips,
    firstTrip,
    pickupTime,
    appointmentTime,
    isWillCall,
    origin: route[0],
    destination: visualDestination(route, sortedTrips.length),
    routeContinuation: routeContinuation(route),
    routeSummary: routeSummary(route, sortedTrips.length),
    status: journeyStatus(sortedTrips),
    sortTime: pickupTime ?? '99:99:99',
  };
}

function visualDestination(route: Address[], tripCount: number): Address {
  if (tripCount === 2 && route.length === 3 && addressKey(route[0]) === addressKey(route[2])) {
    return route[1];
  }

  return route.at(-1)!;
}

function compareJourneys(a: JourneyViewModel, b: JourneyViewModel): number {
  return (
    a.sortTime.localeCompare(b.sortTime) ||
    a.passengerName.localeCompare(b.passengerName) ||
    a.id.localeCompare(b.id)
  );
}

function compareTrips(a: Trip, b: Trip): number {
  return (
    (tripActionTime(a) ?? '99:99:99').localeCompare(tripActionTime(b) ?? '99:99:99') ||
    a.brokerTripNumber.localeCompare(b.brokerTripNumber) ||
    a.id.localeCompare(b.id)
  );
}

function journeyStatus(trips: Trip[]): JourneyStatus {
  const statuses = trips.map(tripStatus);
  const brokerIssue = statuses.find((status) => status.key === 'trips.status.brokerIssue');

  if (brokerIssue) {
    return brokerIssue;
  }

  const willCall = statuses.find((status) => status.key === 'trips.willCall');
  if (willCall) {
    return willCall;
  }

  const needsPickup = statuses.find((status) => status.key === 'trips.status.needsPickupTime');
  if (needsPickup) {
    return needsPickup;
  }

  return { key: 'trips.status.scheduled', variant: 'default' };
}

function routeAddresses(trips: Trip[]): Address[] {
  const route: Address[] = [];

  for (const trip of trips) {
    pushAddress(route, trip.pickup);
    pushAddress(route, trip.dropoff);
  }

  return route;
}

function pushAddress(route: Address[], address: Address): void {
  if (!route.length || addressKey(route.at(-1)!) !== addressKey(address)) {
    route.push(address);
  }
}

function addressKey(address: Address): string {
  return [address.address, address.city, address.state, address.zip]
    .map((part) => part?.trim().toLocaleUpperCase() ?? '')
    .join('|');
}

function routeSummary(route: Address[], tripCount: number): string {
  if (tripCount === 2 && route.length === 3 && addressKey(route[0]) === addressKey(route[2])) {
    return `${route[0].address} ⇄ ${route[1].address}`;
  }

  if (route.length <= 4) {
    return route.map((address) => address.address).join(' → ');
  }

  return `${route[0].address} → ${route[1].address} → +${route.length - 2} stops`;
}

function routeContinuation(route: Address[]): string {
  if (route.length <= 1) {
    return '';
  }

  if (route.length === 3 && addressKey(route[0]) === addressKey(route[2])) {
    return `⇄ ${route[1].address}`;
  }

  if (route.length <= 4) {
    return `→ ${route
      .slice(1)
      .map((address) => address.address)
      .join(' → ')}`;
  }

  return `→ ${route[1].address} → +${route.length - 2} stops`;
}
