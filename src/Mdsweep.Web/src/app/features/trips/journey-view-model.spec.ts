import { buildJourneys } from './journey-view-model';
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
