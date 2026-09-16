import { buildJourneys, matchesJourneyFilter, matchesJourneySearch } from './journey-view-model';
import { Trip } from './trips-types';

describe('Journey view model', () => {
  it('sorts journeys by earliest actionable time with untimed journeys last', () => {
    const journeys = buildJourneys([
      trip({ id: 'late', journeyId: 'late', scheduledPickupTime: '13:00:00' }),
      trip({ id: 'untimed', journeyId: 'untimed', appointmentTime: null }),
      trip({ id: 'early', journeyId: 'early', scheduledPickupTime: '08:00:00' }),
    ]);

    expect(journeys.map((journey) => journey.id)).toEqual(['early', 'late', 'untimed']);
  });

  it('does not present appointment time as pickup time while retaining it as a sort fallback', () => {
    const journey = buildJourneys([
      trip({ appointmentTime: '11:00:00', scheduledPickupTime: null }),
    ])[0];

    expect(journey.displayPickupTime).toBeNull();
    expect(journey.appointmentTime).toBe('11:00:00');
    expect(journey.sortTime).toBe('11:00:00');
  });

  it('places untimed will-call Journeys after actionable pickup/reference times', () => {
    const journeys = buildJourneys([
      trip({
        id: 'will-call',
        journeyId: 'will-call',
        isWillCall: true,
        appointmentTime: '07:00:00',
      }),
      trip({ id: 'reference', journeyId: 'reference', appointmentTime: '10:00:00' }),
    ]);

    expect(journeys.map((journey) => journey.id)).toEqual(['reference', 'will-call']);
    expect(journeys[1].sortTime).toBeNull();
    expect(journeys[1].displayPickupTime).toBeNull();
  });

  it('sorts the weekly view by date before clock time and prefers actual pickup over fallback', () => {
    const journeys = buildJourneys([
      trip({
        id: 'tuesday',
        journeyId: 'tuesday',
        serviceDate: '2026-09-16',
        scheduledPickupTime: '07:00:00',
      }),
      trip({
        id: 'monday',
        journeyId: 'monday',
        serviceDate: '2026-09-15',
        scheduledPickupTime: '15:00:00',
      }),
      trip({
        id: 'reference',
        journeyId: 'monday',
        serviceDate: '2026-09-15',
        appointmentTime: '08:00:00',
      }),
    ]);

    expect(journeys.map((journey) => journey.id)).toEqual(['monday', 'tuesday']);
    expect(journeys[0].displayPickupTime).toBe('15:00:00');
    expect(journeys[0].sortTime).toBe('15:00:00');
  });

  it('keeps lifecycle separate from broker and pickup-time attention', () => {
    const brokerIssue = buildJourneys([
      trip({ id: 'scheduled', scheduledPickupTime: '09:00:00' }),
      trip({ id: 'broker', brokerTripNumber: '1001', brokerStatus: 'TURN BACK' }),
    ])[0];
    const needsPickup = buildJourneys([
      trip({ id: 'will-call', isWillCall: true }),
      trip({ id: 'untimed', brokerTripNumber: '1001' }),
    ])[0];

    expect(brokerIssue.status.key).toBe('trips.status.scheduled');
    expect(brokerIssue.attention?.key).toBe('trips.status.brokerIssue');
    expect(needsPickup.status.key).toBe('trips.willCall');
    expect(needsPickup.attention?.key).toBe('trips.status.needsPickupTime');
    expect(matchesJourneyFilter(needsPickup, 'willCall')).toBe(true);
    expect(matchesJourneyFilter(needsPickup, 'scheduled')).toBe(false);
  });

  it('renders persisted lifecycle values when present without deriving them from pickup time', () => {
    const inProgress = buildJourneys([
      trip({ id: 'active', lifecycleStatus: 'inProgress', scheduledPickupTime: null }),
      trip({ id: 'pending', brokerTripNumber: '1001', scheduledPickupTime: '12:00:00' }),
    ])[0];
    const completed = buildJourneys([
      trip({ id: 'done-1', journeyId: 'done', lifecycleStatus: 'completed' }),
      trip({ id: 'done-2', journeyId: 'done', lifecycleStatus: 'completed' }),
    ])[0];

    expect(inProgress.status.key).toBe('trips.status.inProgress');
    expect(matchesJourneyFilter(inProgress, 'inProgress')).toBe(true);
    expect(completed.status.key).toBe('trips.status.completed');
    expect(matchesJourneyFilter(completed, 'completed')).toBe(true);
    expect(completed.attention?.key).toBe('trips.status.needsPickupTime');
  });

  it('summarizes Trip-owned assignments without assigning the Journey', () => {
    const assignment = { driverName: 'Driver 3', vehicleName: 'Van 2' };
    const shared = buildJourneys([
      trip({ id: 'one', assignment }),
      trip({ id: 'two', assignment }),
    ])[0];
    const partial = buildJourneys([
      trip({ id: 'one', assignment }),
      trip({ id: 'two' }),
    ])[0];
    const different = buildJourneys([
      trip({ id: 'one', assignment }),
      trip({ id: 'two', assignment: { driverName: 'Driver 4', vehicleName: null } }),
    ])[0];

    expect(shared.assignment).toEqual({
      key: 'trips.assignmentSummary',
      params: { driver: 'Driver 3', vehicle: 'Van 2' },
    });
    expect(partial.assignment.key).toBe('trips.partiallyAssigned');
    expect(different.assignment.key).toBe('trips.multipleAssignments');
  });

  it('searches Journey membership without removing non-matching sibling Trips', () => {
    const journey = buildJourneys([
      trip({ id: 'outbound', brokerTripNumber: 'OUT-100' }),
      trip({ id: 'return', brokerTripNumber: 'RETURN-200' }),
    ])[0];

    expect(matchesJourneySearch(journey, 'return-200')).toBe(true);
    expect(journey.trips).toHaveLength(2);
  });

  it('renders reciprocal pairs distinctly from multi-stop journeys', () => {
    const reciprocal = buildJourneys([
      trip({ id: 'one' }),
      trip({
        id: 'two',
        direction: 'From',
        pickup: address('Clinic'),
        dropoff: address('Home'),
      }),
    ])[0];
    const multiStop = buildJourneys([
      trip({ id: 'a', journeyId: 'multi', pickup: address('A'), dropoff: address('B') }),
      trip({ id: 'b', journeyId: 'multi', pickup: address('B'), dropoff: address('C') }),
      trip({ id: 'c', journeyId: 'multi', pickup: address('C'), dropoff: address('A') }),
    ])[0];

    expect(reciprocal.routeSummary).toBe('Home ⇄ Clinic');
    expect(multiStop.routeSummary).toBe('A → B → C → A');
  });
});

function address(value: string) {
  return { address: value, city: 'Phoenix', state: 'AZ', zip: null };
}

function trip(overrides: Partial<Trip>): Trip {
  return {
    id: 'trip',
    journeyId: 'journey',
    brokerTripNumber: '1000',
    passengerFirstName: 'Synthetic',
    passengerLastName: 'Passenger',
    memberId: null,
    serviceDate: '2026-09-15',
    direction: 'To',
    brokerStatus: 'VALID',
    isWillCall: false,
    passengerType: null,
    specialNeeds: null,
    tripCost: null,
    tripMileage: null,
    appointmentTime: null,
    returnPickupTime: null,
    scheduledPickupTime: null,
    calculatedPickupTime: null,
    manualPickupTime: null,
    estimatedTravelMinutes: null,
    estimatedDistanceMeters: null,
    pickup: address('Home'),
    dropoff: address('Clinic'),
    ...overrides,
  };
}
