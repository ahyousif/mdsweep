import { tripReferenceTime } from './trip-reference-time';
import { Address, Trip } from './trips-types';

export type JourneyFilter = 'all' | 'scheduled' | 'inProgress' | 'completed' | 'willCall';

export type JourneyStatus = {
  key: string;
  params?: Record<string, string>;
  variant: 'default' | 'secondary' | 'outline';
};

export type JourneyAttention = {
  key: string;
  params?: Record<string, string>;
};

export type AssignmentSummary = {
  key: string;
  params?: Record<string, string>;
};

export type JourneyViewModel = {
  id: string;
  passengerName: string;
  memberId: string | null;
  trips: Trip[];
  firstTrip: Trip;
  displayPickupTime: string | null;
  appointmentTime: string | null;
  isWillCall: boolean;
  isEntirelyWillCall: boolean;
  origin: Address;
  destination: Address;
  routeContinuation: string;
  routeSummary: string;
  status: JourneyStatus;
  attention: JourneyAttention | null;
  assignment: AssignmentSummary;
  sortTime: string | null;
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
    case 'inProgress':
      return journey.status.key === 'trips.status.inProgress';
    case 'completed':
      return journey.status.key === 'trips.status.completed';
    case 'willCall':
      return journey.isWillCall;
  }
}

export function matchesJourneySearch(journey: JourneyViewModel, search: string): boolean {
  const query = search.trim().toLocaleUpperCase();

  if (!query) {
    return true;
  }

  return journey.trips.some((trip) =>
    [
      trip.brokerTripNumber,
      trip.passengerFirstName,
      trip.passengerLastName,
      trip.memberId,
      trip.pickup.address,
      trip.pickup.city,
      trip.dropoff.address,
      trip.dropoff.city,
    ].some((value) => value?.toLocaleUpperCase().includes(query)),
  );
}

export function tripActionTime(trip: Trip): string | null {
  return trip.scheduledPickupTime ?? (trip.isWillCall ? null : tripReferenceTime(trip).value);
}

export function tripStatus(trip: Trip): JourneyStatus {
  switch (trip.lifecycleStatus) {
    case 'inProgress':
      return { key: 'trips.status.inProgress', variant: 'default' };
    case 'completed':
      return { key: 'trips.status.completed', variant: 'outline' };
    default:
      // The current API omits lifecycle. Treat an imported, unexecuted Trip as provisionally
      // Scheduled regardless of whether a pickup time has been set; Will call stays distinct.
      return trip.isWillCall && !trip.scheduledPickupTime
        ? { key: 'trips.willCall', variant: 'outline' }
        : { key: 'trips.status.scheduled', variant: 'secondary' };
  }
}

export function tripAssignmentSummary(trip: Trip): AssignmentSummary {
  return trip.assignment
    ? {
        key: trip.assignment.vehicleName ? 'trips.assignmentSummary' : 'trips.driverOnly',
        params: {
          driver: trip.assignment.driverName,
          vehicle: trip.assignment.vehicleName ?? '',
        },
      }
    : { key: 'trips.unassigned' };
}

function toJourney(id: string, trips: Trip[]): JourneyViewModel {
  const sortedTrips = [...trips].sort(compareTrips);
  const firstTrip = sortedTrips[0];
  const route = routeAddresses(sortedTrips);
  const displayPickupTime = earliestTime(sortedTrips.map((trip) => trip.scheduledPickupTime));
  const sortTime = displayPickupTime ?? earliestTime(sortedTrips.map(tripActionTime));
  const appointmentTime = sortedTrips.map((trip) => trip.appointmentTime).find(Boolean) ?? null;
  const isWillCall = sortedTrips.some((trip) => trip.isWillCall && !trip.scheduledPickupTime);
  const isEntirelyWillCall = sortedTrips.every(
    (trip) => trip.isWillCall && !trip.scheduledPickupTime,
  );

  return {
    id,
    passengerName: `${firstTrip.passengerFirstName} ${firstTrip.passengerLastName}`,
    memberId: firstTrip.memberId,
    trips: sortedTrips,
    firstTrip,
    displayPickupTime,
    appointmentTime,
    isWillCall,
    isEntirelyWillCall,
    origin: route[0],
    destination: visualDestination(route, sortedTrips.length),
    routeContinuation: routeContinuation(route),
    routeSummary: routeSummary(route, sortedTrips.length),
    status: journeyStatus(sortedTrips),
    attention: journeyAttention(sortedTrips),
    assignment: journeyAssignment(sortedTrips),
    sortTime,
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
    Number(b.sortTime !== null) - Number(a.sortTime !== null) ||
    a.firstTrip.serviceDate.localeCompare(b.firstTrip.serviceDate) ||
    (a.sortTime ?? '').localeCompare(b.sortTime ?? '') ||
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
  if (statuses.some((status) => status.key === 'trips.status.inProgress')) {
    return { key: 'trips.status.inProgress', variant: 'default' };
  }
  if (statuses.every((status) => status.key === 'trips.status.completed')) {
    return { key: 'trips.status.completed', variant: 'outline' };
  }
  if (statuses.some((status) => status.key === 'trips.willCall')) {
    return { key: 'trips.willCall', variant: 'outline' };
  }
  return { key: 'trips.status.scheduled', variant: 'secondary' };
}

function journeyAttention(trips: Trip[]): JourneyAttention | null {
  const brokerIssue = trips.find(isBrokerIssue);
  if (brokerIssue) {
    return { key: 'trips.status.brokerIssue', params: { status: brokerIssue.brokerStatus! } };
  }
  return trips.some((trip) => !trip.isWillCall && trip.scheduledPickupTime === null)
    ? { key: 'trips.status.needsPickupTime' }
    : null;
}

function journeyAssignment(trips: Trip[]): AssignmentSummary {
  const assignments = trips.map((trip) => trip.assignment);
  if (assignments.every((assignment) => !assignment)) {
    return { key: 'trips.unassigned' };
  }
  if (assignments.some((assignment) => !assignment)) {
    return { key: 'trips.partiallyAssigned' };
  }
  const first = assignments[0]!;
  return assignments.every(
    (assignment) =>
      assignment?.driverName === first.driverName &&
      assignment?.vehicleName === first.vehicleName,
  )
    ? tripAssignmentSummary(trips[0])
    : { key: 'trips.multipleAssignments' };
}

function earliestTime(times: Array<string | null>): string | null {
  return times.filter((time): time is string => time !== null).sort()[0] ?? null;
}

function isBrokerIssue(trip: Trip): boolean {
  return Boolean(trip.brokerStatus && trip.brokerStatus.toUpperCase() !== 'VALID');
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
