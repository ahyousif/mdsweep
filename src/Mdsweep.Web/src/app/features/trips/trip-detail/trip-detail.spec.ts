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
      lifecycleStatus: 'completed',
      assignment: { driverName: 'Driver 3', vehicleName: 'Van 2' },
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
    expect(fixture.nativeElement.textContent).toContain('Journey trips');
    // Four Overview cards plus the Passenger tab's card (kept in the DOM while inactive).
    expect(fixture.nativeElement.querySelectorAll('[hlmCard]')).toHaveLength(5);
    expect(legRows[0].textContent).not.toContain('10:00 AM');
    expect(legRows[1].textContent).not.toContain('Not supplied');
    expect(legRows[0].textContent).toContain('Unassigned');
    expect(legRows[1].textContent).toContain('Driver 3 · Van 2');
    expect(legRows[1].textContent).toContain('Completed');
    expect(legRows[0].querySelector('[data-slot="badge"]')?.getAttribute('data-variant')).toBe(
      'secondary',
    );
    expect(legRows[1].querySelector('[data-slot="badge"]')?.getAttribute('data-variant')).toBe(
      'outline',
    );
    expect(legRows[0].querySelector('[data-trip-pickup]')?.textContent).toContain('100 Home St');
    expect(legRows[0].querySelector('[data-trip-dropoff]')?.textContent).toContain('200 Clinic Ave');
    expect(legRows[0].getAttribute('aria-pressed')).toBe('true');
    expect(legRows[1].getAttribute('aria-pressed')).toBe('false');
    const tripButtons = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    );
    expect(tripButtons.find((button) => button.textContent?.includes('Assign all'))?.disabled).toBe(
      true,
    );
    expect(tripButtons.find((button) => button.textContent?.includes('Add trip'))?.disabled).toBe(
      true,
    );
    expect(
      fixture.nativeElement.querySelectorAll('ng-icon[name="lucideEllipsisVertical"]'),
    ).toHaveLength(2);
    expect(
      fixture.nativeElement
        .querySelector('[data-selected-trip-id]')
        ?.getAttribute('data-selected-trip-id'),
    ).toBe(outbound.id);
    legRows[1].click();
    await fixture.whenStable();
    expect(selected).toHaveBeenCalledWith(inbound);

    fixture.componentRef.setInput('selectedTripId', inbound.id);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(legRows[0].getAttribute('aria-pressed')).toBe('false');
    expect(legRows[1].getAttribute('aria-pressed')).toBe('true');
    const selectedAssignment = fixture.nativeElement.querySelector(
      '[data-selected-trip-id]',
    ) as HTMLElement;
    expect(selectedAssignment.getAttribute('data-selected-trip-id')).toBe(inbound.id);
    expect(selectedAssignment.closest('[hlmCard]')?.textContent).toContain('1:30 PM');
    expect(selectedAssignment.closest('[hlmCard]')?.textContent).toContain('200 Clinic Ave');
    expect(selectedAssignment.closest('[hlmCard]')?.textContent).toContain('Assignment');
    expect(selectedAssignment.textContent).toContain('Driver 3');
    expect(selectedAssignment.textContent).toContain('Van 2');
    const changeButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Change pickup time'))!;
    changeButton.click();
    expect(pickupChange).toHaveBeenCalledWith(inbound);
  });

  it('shows an untimed will-call return without borrowing its appointment time', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripDetail);
    fixture.componentRef.setInput(
      'journey',
      buildJourneys([
        trip({
          direction: 'From',
          isWillCall: true,
          scheduledPickupTime: null,
          appointmentTime: '10:00:00',
        }),
      ])[0],
    );
    fixture.detectChanges();
    await fixture.whenStable();

    const row = fixture.nativeElement.querySelector('[data-trip-row]') as HTMLElement;
    expect(row.textContent).toContain('Will call');
    expect(row.textContent).not.toContain('10:00 AM');
    expect(row.querySelector('[data-trip-dropoff]')?.textContent).toContain('200 Clinic Ave');
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

  it('shows reciprocal addresses once in the compact Journey card', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripDetail);
    const outbound = trip({ id: 'outbound' });
    const returning = trip({
      id: 'return',
      brokerTripNumber: '1001',
      direction: 'From',
      appointmentTime: null,
      returnPickupTime: '13:30:00',
      scheduledPickupTime: '13:30:00',
      pickup: outbound.dropoff,
      dropoff: outbound.pickup,
    });
    fixture.componentRef.setInput('journey', buildJourneys([outbound, returning])[0]);
    fixture.detectChanges();
    await fixture.whenStable();

    const card = fixture.nativeElement.querySelector('[data-journey-card]') as HTMLElement;
    const route = card.querySelector('[data-journey-route]') as HTMLElement;
    expect(route.textContent?.match(/100 Home St/g)).toHaveLength(1);
    expect(route.textContent?.match(/200 Clinic Ave/g)).toHaveLength(1);
    expect(card.textContent).toContain('First pickup');
    expect(card.textContent).toContain('Total trips');
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
