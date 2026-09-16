import { provideLocalization } from '@app/core/i18n/localization.providers';
import { TestBed } from '@angular/core/testing';

import { buildJourneys } from '../journey-view-model';
import { Trip } from '../trips-types';
import TripDetail from './trip-detail';

describe('Journey detail', () => {
  it('keeps each leg selectable and edits pickup time for the active trip', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripDetail);
    const outbound = trip({ id: 'outbound', brokerTripNumber: '1001' });
    const inbound = trip({
      id: 'inbound',
      brokerTripNumber: '1002',
      direction: 'From',
      appointmentTime: null,
      returnPickupTime: '13:30:00',
      scheduledPickupTime: '13:30:00',
      pickup: outbound.dropoff,
      dropoff: outbound.pickup,
    });
    const journey = buildJourneys([outbound, inbound])[0];
    fixture.componentRef.setInput('journey', journey);
    fixture.componentRef.setInput('selectedTripId', outbound.id);
    const selected = vi.fn();
    const pickupChange = vi.fn();
    fixture.componentInstance.tripSelected.subscribe(selected);
    fixture.componentInstance.changeScheduledPickup.subscribe(pickupChange);

    fixture.detectChanges();
    await fixture.whenStable();

    const legRows = fixture.nativeElement.querySelectorAll(
      '[data-trip-row]',
    ) as NodeListOf<HTMLElement>;
    expect(legRows).toHaveLength(2);
    legRows[1].click();
    await fixture.whenStable();
    expect(selected).toHaveBeenCalledWith(inbound);

    fixture.componentRef.setInput('selectedTripId', inbound.id);
    fixture.detectChanges();
    await fixture.whenStable();
    const changeButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Change pickup time'))!;
    changeButton.click();
    expect(pickupChange).toHaveBeenCalledWith(inbound);
  });

  it('builds a safe map link for the journey route', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripDetail);
    const journey = buildJourneys([trip({})])[0];
    fixture.componentRef.setInput('journey', journey);
    fixture.componentRef.setInput('selectedTripId', journey.firstTrip.id);
    fixture.detectChanges();
    await fixture.whenStable();

    const link = fixture.nativeElement.querySelector('a') as HTMLAnchorElement;
    const url = new URL(link.href);
    expect(url.origin).toBe('https://www.google.com');
    expect(url.pathname).toBe('/maps/dir/');
    expect(url.searchParams.get('origin')).toBe('100 Home St, Phoenix, AZ, 85001');
    expect(url.searchParams.get('destination')).toBe('200 Clinic Ave, Mesa, AZ, 85201');
    expect(link.rel).toBe('noopener noreferrer');
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
