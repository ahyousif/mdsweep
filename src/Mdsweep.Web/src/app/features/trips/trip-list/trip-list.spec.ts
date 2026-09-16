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
    const header = fixture.nativeElement.querySelector('[data-journey-header]') as HTMLElement;
    expect(header).not.toBeNull();
    expect(header.textContent).toContain('Pickup / Appt');
    expect(rows[0].textContent).toContain('2 trips');
    const card = fixture.nativeElement.querySelector('app-trip-card') as HTMLElement;
    expect(card.querySelector('ng-icon[name="lucideChevronRight"]')).toBeNull();
    expect(card.querySelector('ng-icon[name="lucideEllipsisVertical"]')).not.toBeNull();

    rows[0].click();
    await fixture.whenStable();

    expect(selected).toHaveBeenCalledWith(journeys[0]);
  });

  it('omits column headers when there are no journeys', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripList);
    fixture.componentRef.setInput('journeys', []);
    fixture.detectChanges();
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('[data-journey-header]')).toBeNull();
    expect(fixture.nativeElement.querySelector('[data-journey-row]')).toBeNull();
  });

  it('shows the appointment beneath an unset pickup instead of using it as pickup', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripList);
    fixture.componentRef.setInput('journeys', buildJourneys([trip({})]));
    fixture.detectChanges();
    await fixture.whenStable();

    const row = fixture.nativeElement.querySelector('[data-journey-row]') as HTMLElement;
    expect(row.querySelector('p')?.textContent?.trim()).toBe('Not set');
    expect(row.textContent).toContain('Appt 10:00 AM');
    const badge = row.querySelector('[data-slot="badge"]') as HTMLElement;
    expect(badge.textContent).toContain('Scheduled');
    expect(badge.getAttribute('data-variant')).toBe('secondary');
    expect(row.textContent).toContain('Needs pickup time');
  });

  it('keeps broker issues secondary to a lifecycle badge', async () => {
    TestBed.configureTestingModule({ providers: [provideLocalization()] });
    const fixture = TestBed.createComponent(TripList);
    fixture.componentRef.setInput(
      'journeys',
      buildJourneys([trip({ brokerStatus: 'TURN BACK', scheduledPickupTime: '09:00:00' })]),
    );
    fixture.detectChanges();
    await fixture.whenStable();

    const row = fixture.nativeElement.querySelector('[data-journey-row]') as HTMLElement;
    expect(row.querySelector('[data-slot="badge"]')?.textContent).toContain('Scheduled');
    expect(row.textContent).toContain('Broker: TURN BACK');
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
