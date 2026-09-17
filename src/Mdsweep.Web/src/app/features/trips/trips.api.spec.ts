import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { buildJourneys, matchesJourneySearch } from './journey-view-model';
import { Trip } from './trips-types';
import { TripsApi } from './trips.api';

describe('TripsApi Journey grouping query', () => {
  let api: TripsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(TripsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('loads every date-window page without server-side search before grouping', async () => {
    const outbound = trip({ id: 'outbound', brokerTripNumber: 'OUT-100' });
    const returning = trip({ id: 'return', brokerTripNumber: 'RETURN-200' });
    const otherTrips = Array.from({ length: 99 }, (_, index) =>
      trip({ id: `other-${index}`, journeyId: `other-journey-${index}` }),
    );
    const loading = api.getAllTrips({
      startDate: '2026-09-14',
      endDate: '2026-09-20',
      search: 'RETURN-200',
    });

    const first = http.expectOne(
      (request) => request.url === '/api/trips' && request.params.get('page') === '1',
    );
    expect(first.request.params.get('pageSize')).toBe('100');
    expect(first.request.params.get('startDate')).toBe('2026-09-14');
    expect(first.request.params.get('endDate')).toBe('2026-09-20');
    expect(first.request.params.has('search')).toBe(false);
    first.flush({
      items: [outbound, ...otherTrips],
      totalCount: 101,
      page: 1,
      pageSize: 100,
      totalPages: 2,
    });

    await Promise.resolve();
    const second = http.expectOne(
      (request) => request.url === '/api/trips' && request.params.get('page') === '2',
    );
    expect(second.request.params.get('startDate')).toBe('2026-09-14');
    expect(second.request.params.get('endDate')).toBe('2026-09-20');
    second.flush({
      items: [returning],
      totalCount: 101,
      page: 2,
      pageSize: 100,
      totalPages: 2,
    });

    const trips = await loading;
    expect(trips).toHaveLength(101);
    const journey = buildJourneys(trips).find((item) => item.id === outbound.journeyId)!;
    expect(journey.trips).toHaveLength(2);
    expect(matchesJourneySearch(journey, 'RETURN-200')).toBe(true);
    expect(journey.trips.map((item) => item.brokerTripNumber)).toContain('OUT-100');
  });

  it('fails instead of silently showing an incomplete Journey when pages shift', async () => {
    const loading = api.getAllTrips({ startDate: '2026-09-15', endDate: '2026-09-15' });
    http
      .expectOne((request) => request.params.get('page') === '1')
      .flush({
        items: [trip({ id: 'outbound' })],
        totalCount: 101,
        page: 1,
        pageSize: 100,
        totalPages: 2,
      });

    await Promise.resolve();
    http
      .expectOne((request) => request.params.get('page') === '2')
      .flush({
        items: [trip({ id: 'outbound' })],
        totalCount: 101,
        page: 2,
        pageSize: 100,
        totalPages: 2,
      });

    await expect(loading).rejects.toThrow('retry to load complete Journeys');
  });
});

function trip(overrides: Partial<Trip>): Trip {
  return {
    id: 'trip',
    journeyId: 'journey-1',
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
    appointmentTime: '10:00:00',
    returnPickupTime: null,
    scheduledPickupTime: '09:00:00',
    calculatedPickupTime: null,
    manualPickupTime: null,
    estimatedTravelMinutes: null,
    estimatedDistanceMeters: null,
    pickup: { address: '100 Home St', city: 'Phoenix', state: 'AZ', zip: '85001' },
    dropoff: { address: '200 Clinic Ave', city: 'Mesa', state: 'AZ', zip: '85201' },
    ...overrides,
  };
}
