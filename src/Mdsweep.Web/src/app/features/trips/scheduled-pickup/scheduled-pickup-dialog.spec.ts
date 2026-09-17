import { TestBed } from '@angular/core/testing';
import { DIALOG_DATA } from '@angular/cdk/dialog';
import { BrnDialogRef } from '@spartan-ng/brain/dialog';
import { QueryObserver } from '@tanstack/query-core';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { provideLocalization } from '@app/core/i18n/localization.providers';
import { LanguageService } from '@app/core/i18n/language.service';
import { Trip } from '../trips-types';
import { TripsApi } from '../trips.api';
import ScheduledPickupDialog from './scheduled-pickup-dialog';

const trip: Trip = {
  id: 'synthetic-trip',
  journeyId: 'synthetic-journey',
  brokerTripNumber: 'SYN-1',
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
  appointmentTime: '05:00:00',
  returnPickupTime: null,
  scheduledPickupTime: '04:10:00',
  calculatedPickupTime: '04:03:00',
  manualPickupTime: '04:10:00',
  estimatedTravelMinutes: 42,
  estimatedDistanceMeters: 47475,
  pickup: { address: '100 Sample St', city: 'Phoenix', state: 'AZ', zip: '85001' },
  dropoff: { address: '200 Synthetic Way', city: 'Mesa', state: 'AZ', zip: '85201' },
};

function setup(value = trip) {
  const client = new QueryClient({
    defaultOptions: { mutations: { retry: false }, queries: { retry: false } },
  });
  const api = { setScheduledPickupTime: vi.fn().mockResolvedValue({}) };
  const close = vi.fn();
  TestBed.configureTestingModule({
    providers: [
      provideLocalization(),
      provideTanStackQuery(client),
      { provide: DIALOG_DATA, useValue: { trip: value } },
      { provide: BrnDialogRef, useValue: { close } },
      { provide: TripsApi, useValue: api },
    ],
  });
  TestBed.inject(LanguageService).setLanguage('en');
  const fixture = TestBed.createComponent(ScheduledPickupDialog);
  fixture.detectChanges();
  return { fixture, component: fixture.componentInstance, api, close, client };
}

describe('Scheduled pickup dialog', () => {
  it.each([
    ['To', false, 'Appointment', '5:00 AM'],
    ['From', false, 'Return pickup', '9:30 AM'],
    ['From', true, 'Return pickup', 'Will call'],
  ] as const)(
    'shows the broker reference for %s (will-call: %s)',
    (direction, isWillCall, label, value) => {
      const { fixture } = setup({ ...trip, direction, isWillCall, returnPickupTime: '09:30:00' });
      const summary = fixture.nativeElement.querySelector('hlm-dialog-header').nextElementSibling;
      expect(summary.textContent).toContain(label);
      expect(summary.textContent).toContain(value);
      if (direction === 'From') expect(summary.textContent).not.toContain('Appointment');
      if (isWillCall) expect(summary.textContent).not.toContain('9:30 AM');
      TestBed.inject(LanguageService).setLanguage('ar');
      fixture.detectChanges();
      expect(summary.textContent).toContain(direction === 'To' ? 'الموعد' : 'اصطحاب العودة');
      if (isWillCall) expect(summary.textContent).toContain('عند الاتصال');
    },
  );

  it('uses a styled text field and a decorative clock without a native picker', () => {
    const { fixture } = setup();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.type).toBe('text');
    expect(input.value).toBe('4:10 AM');
    expect(fixture.nativeElement.querySelector('button[hlmInputGroupButton]')).toBeNull();
    expect(
      fixture.nativeElement
        .querySelector('[hlmInputGroupAddon] ng-icon')
        .getAttribute('aria-hidden'),
    ).toBe('true');
  });

  it.each([
    ['4:15 am', '4:15 AM', '04:15:00'],
    ['4:15PM', '4:15 PM', '16:15:00'],
    ['4 p.m.', '4:00 PM', '16:00:00'],
    [' 04:15 AM ', '4:15 AM', '04:15:00'],
    ['16:15', '4:15 PM', '16:15:00'],
    ['1615', '4:15 PM', '16:15:00'],
    ['415pm', '4:15 PM', '16:15:00'],
    ['0:00', '12:00 AM', '00:00:00'],
    ['12 am', '12:00 AM', '00:00:00'],
    ['12pm', '12:00 PM', '12:00:00'],
    ['23:59', '11:59 PM', '23:59:00'],
    ['4:15 ص', '4:15 AM', '04:15:00'],
    ['4:15 م', '4:15 PM', '16:15:00'],
  ])(
    'normalizes %s on blur and saves the corresponding LocalTime',
    async (value, display, localTime) => {
      const { fixture, api, close } = setup();
      const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
      input.value = value;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
      input.dispatchEvent(new Event('blur'));
      fixture.detectChanges();
      expect(input.value).toBe(display);
      fixture.nativeElement
        .querySelector('form')
        .dispatchEvent(new Event('submit', { cancelable: true }));
      await vi.waitFor(() => expect(close).toHaveBeenCalled());
      expect(api.setScheduledPickupTime).toHaveBeenCalledWith(trip.id, localTime);
    },
  );

  it.each([
    '',
    ' ',
    '24:00',
    '12:60 PM',
    '0 AM',
    '13pm',
    '-1:30',
    '4:5',
    'tomorrow',
    '4:15 AM extra',
  ])('rejects invalid input %s without saving', (value) => {
    const { fixture, component, api } = setup();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();
    component.save();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('hlm-field-error').textContent).toContain(
      'Enter a valid time',
    );
    expect(api.setScheduledPickupTime).not.toHaveBeenCalled();
  });

  it('preserves the time during live language switching and accepts Arabic display on resubmission', async () => {
    const { fixture, component, api, close } = setup();
    TestBed.inject(LanguageService).setLanguage('ar');
    component.normalizeTime();
    fixture.detectChanges();
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.value).toContain('ص');
    expect(input.value).toContain('4:10');
    component.save();
    await vi.waitFor(() => expect(close).toHaveBeenCalled());
    expect(api.setScheduledPickupTime).toHaveBeenCalledWith(trip.id, '04:10:00');
  });

  it('initializes from effective time and saves selected time, invalidating active Trip data', async () => {
    const { fixture, api, close, client } = setup();
    const invalidate = vi.spyOn(client, 'invalidateQueries');
    const fetchTrips = vi.fn().mockResolvedValue({ items: [trip] });
    const observer = new QueryObserver(client, {
      queryKey: ['trips', 'list', {}],
      queryFn: fetchTrips,
    });
    const unsubscribe = observer.subscribe(() => {});
    await vi.waitFor(() => expect(fetchTrips).toHaveBeenCalledTimes(1));
    const input = fixture.nativeElement.querySelector('input') as HTMLInputElement;
    expect(input.value).toBe('4:10 AM');
    input.value = '4:20 am';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { cancelable: true }));
    await vi.waitFor(() => expect(close).toHaveBeenCalled());
    expect(api.setScheduledPickupTime).toHaveBeenCalledWith(trip.id, '04:20:00');
    expect(invalidate).toHaveBeenCalledWith({ queryKey: ['trips'] });
    expect(fetchTrips).toHaveBeenCalledTimes(2);
    unsubscribe();
  });

  it('reset sends null and closes after success', async () => {
    const { fixture, api, close } = setup();
    const reset = [...fixture.nativeElement.querySelectorAll('button')].find((button: unknown) =>
      (button as HTMLButtonElement).textContent?.includes('Use calculated'),
    ) as HTMLButtonElement;
    reset.click();
    await vi.waitFor(() => expect(close).toHaveBeenCalled());
    expect(api.setScheduledPickupTime).toHaveBeenCalledWith(trip.id, null);
  });

  it('disables actions while saving and keeps a localized error open on failure', async () => {
    const { fixture, component, api, close } = setup();
    let reject!: (reason: Error) => void;
    api.setScheduledPickupTime.mockImplementation(
      () =>
        new Promise((_, rejectPromise) => {
          reject = rejectPromise;
        }),
    );
    component.save();
    await vi.waitFor(() => expect(component.saving()).toBe(true));
    fixture.detectChanges();
    expect(
      [...fixture.nativeElement.querySelectorAll('button')].every(
        (button: unknown) => (button as HTMLButtonElement).disabled,
      ),
    ).toBe(true);
    component.cancel();
    expect(close).not.toHaveBeenCalled();
    reject(new Error('Synthetic failure'));
    await vi.waitFor(() => expect(component.mutation.isError()).toBe(true));
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain(
      'Unable to save',
    );
    TestBed.inject(LanguageService).setLanguage('ar');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[role="alert"]').textContent).toContain('تعذر');
    expect(close).not.toHaveBeenCalled();
  });

  it('does not invent a will-call time or label calculated time as manual', () => {
    const { fixture } = setup({
      ...trip,
      isWillCall: true,
      direction: 'From',
      appointmentTime: null,
      scheduledPickupTime: null,
      manualPickupTime: null,
      calculatedPickupTime: null,
    });
    expect(fixture.nativeElement.querySelector('input').value).toBe('');
    expect(fixture.nativeElement.textContent).not.toContain('Manual override');
    expect(fixture.nativeElement.querySelector('button[type="submit"]').disabled).toBe(true);
  });
});
