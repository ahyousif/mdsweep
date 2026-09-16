import { provideLocalization } from '@app/core/i18n/localization.providers';
import { TestBed } from '@angular/core/testing';

import { buildJourneys } from '../journey-view-model';
import { Trip } from '../trips-types';
import TripList from './trip-list';

describe('Trip journey list', () => {
  it('renders one selectable row for all legs in a journey', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripList);
    const journeys = buildJourneys([
      trip({ id: 'inbound', direction: 'From', scheduledPickupTime: '13:30:00' }),
      trip({ id: 'outbound', scheduledPickupTime: '08:30:00' }),
    ]);
    fixture.componentRef.setInput('journeys', journeys);
    const selected = vi.fn();
    fixture.componentInstance.journeySelected.subscribe(selected);

    fixture.detectChanges();
    await fixture.whenStable();

    const rows = fixture.nativeElement.querySelectorAll('[data-journey-row]');
    expect(rows).toHaveLength(1);
    expect(rows[0].textContent).toContain('2 trips');

    rows[0].click();
    await fixture.whenStable();

    expect(selected).toHaveBeenCalledWith(journeys[0]);
  });
});

function trip(overrides: Partial<Trip>): Trip {
  return {
    id: 'trip',
    journeyId: 'journey-1',
    brokerTripNumber: '1000',
    passengerFirstName: 'Synthetic',
    passengerLastName: 'Passenger',
    memberId: 'MED-100',
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
    scheduledPickupTime: null,
    calculatedPickupTime: null,
    manualPickupTime: null,
    estimatedTravelMinutes: null,
    estimatedDistanceMeters: null,
    pickup: { address: '100 Home St', city: 'Phoenix', state: 'AZ', zip: '85001' },
    dropoff: { address: '200 Clinic Ave', city: 'Mesa', state: 'AZ', zip: '85201' },
    ...overrides,
  };
}
