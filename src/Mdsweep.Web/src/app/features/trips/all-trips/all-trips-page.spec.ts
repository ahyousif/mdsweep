import { BreakpointObserver } from '@angular/cdk/layout';
import { BehaviorSubject } from 'rxjs';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { ApplicationError } from '@app/core/errors/application-error';
import { AllTripsApi, AllTripsTrip, TripsResponse } from './all-trips.api';
import AllTripsPage from './all-trips-page';

describe('AllTripsPage', () => {
  let fixture: ComponentFixture<AllTripsPage>;
  const api = {
    getTrips: vi.fn(),
    setScheduledPickupTime: vi.fn(),
  };

  beforeEach(() => {
    api.getTrips.mockReset();
    api.setScheduledPickupTime.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(
          new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } }),
        ),
        provideRouter([]),
        { provide: AllTripsApi, useValue: api },
      ],
    });
    fixture = TestBed.createComponent(AllTripsPage);
  });

  it('starts at today, supports day navigation, and shows a query failure separately', async () => {
    api.getTrips.mockRejectedValue(new ApplicationError('Trips are temporarily unavailable.', 503));
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('hlm-date-picker')).not.toBeNull();
    const today = fixture.componentInstance.serviceDate();
    expect(fixture.componentInstance.isToday()).toBe(true);
    fixture.componentInstance.setServiceDate(new Date(2026, 8, 2));
    fixture.componentInstance.moveServiceDate(1);
    expect(fixture.componentInstance.serviceDate()).toBe('2026-09-03');
    fixture.detectChanges();

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(api.getTrips).toHaveBeenCalledWith(
        expect.objectContaining({ startDate: '2026-09-03', endDate: '2026-09-03' }),
      );
      expect(fixture.nativeElement.textContent).toContain('Trips could not be loaded');
      expect(fixture.nativeElement.textContent).toContain('Trips are temporarily unavailable.');
    });

    fixture.componentInstance.goToToday();
    expect(fixture.componentInstance.serviceDate()).toBe(today);
  });
});

describe('Trips workspace interactions', () => {
  let fixture: ComponentFixture<AllTripsPage>;
  let viewport: BehaviorSubject<{ matches: boolean; breakpoints: Record<string, boolean> }>;
  const api = { getTrips: vi.fn(), setScheduledPickupTime: vi.fn() };
  const trip: AllTripsTrip = {
    id: 'synthetic-trip',
    brokerTripNumber: 'SYN-001',
    passengerFirstName: 'Synthetic',
    passengerLastName: 'Passenger',
    brokerMemberId: null,
    serviceDate: '2026-09-04',
    appointmentTime: '10:00:00',
    brokerStatus: 'VALID',
    isWillCall: false,
    mobilityRequirement: 'Unknown',
    tripCost: null,
    tripMileage: null,
    scheduledPickupTime: null,
    pickupAddress: '100 Sample St',
    pickupCity: 'Phoenix',
    dropoffAddress: '200 Synthetic Way',
    dropoffCity: 'Mesa',
  };
  const response = (items = [trip]): TripsResponse => ({
    items,
    totalCount: items.length,
    scopeCount: items.length,
    attentionCount: items.length,
    page: 1,
    pageSize: 50,
    totalPages: 1,
  });
  const button = (text: string) =>
    Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((item) => item.textContent?.trim() === text)!;
  const settle = async () => {
    fixture.autoDetectChanges();
    await fixture.whenStable();
    await vi.waitFor(() => expect(fixture.componentInstance.tripsQuery.isFetching()).toBe(false));
    await fixture.whenStable();
  };

  beforeEach(() => {
    vi.stubGlobal(
      'ResizeObserver',
      class {
        observe() {}
        disconnect() {}
      },
    );
    api.getTrips.mockReset().mockResolvedValue(response());
    api.setScheduledPickupTime.mockReset().mockResolvedValue(undefined);
    viewport = new BehaviorSubject<{ matches: boolean; breakpoints: Record<string, boolean> }>({
      matches: true,
      breakpoints: {},
    });
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(
          new QueryClient({ defaultOptions: { queries: { retry: false, gcTime: Infinity } } }),
        ),
        provideRouter([]),
        { provide: AllTripsApi, useValue: api },
        { provide: BreakpointObserver, useValue: { observe: () => viewport.asObservable() } },
      ],
    });
    fixture = TestBed.createComponent(AllTripsPage);
  });

  afterEach(() => vi.unstubAllGlobals());

  it('uses a skeleton initially, keeps previous rows during fetches and failures, and retries', async () => {
    const initial = deferred<TripsResponse>();
    api.getTrips.mockReturnValueOnce(initial.promise);
    fixture.autoDetectChanges();
    expect(fixture.nativeElement.querySelector('[aria-label="Loading trips"]')).not.toBeNull();
    initial.resolve(response());
    await settle();
    const update = deferred<TripsResponse>();
    api.getTrips.mockReturnValueOnce(update.promise);
    button('Tomorrow').click();
    await vi.waitFor(() => expect(fixture.nativeElement.textContent).toContain('Updating…'));
    expect(fixture.nativeElement.querySelector('tbody')?.textContent).toContain(
      'Synthetic Passenger',
    );
    expect(fixture.nativeElement.textContent).toContain('Updating…');
    expect(fixture.nativeElement.querySelector('[aria-label="Loading trips"]')).toBeNull();
    update.reject(new ApplicationError('Please retry.', 503));
    await settle();
    expect(fixture.nativeElement.querySelector('tbody')?.textContent).toContain(
      'Synthetic Passenger',
    );
    expect(fixture.nativeElement.textContent).toContain('Trips could not be updated');
    button('Retry').click();
    await settle();
    await vi.waitFor(() =>
      expect(fixture.nativeElement.textContent).not.toContain('Trips could not be updated'),
    );
  });

  it('debounces search, resets pagination and clears immediately', async () => {
    await settle();
    fixture.componentInstance.setPage(2);
    await settle();
    api.getTrips.mockClear();
    const input = fixture.nativeElement.querySelector('input[type="search"]') as HTMLInputElement;
    input.value = 'Syn';
    input.dispatchEvent(new Event('input'));
    await new Promise((resolve) => setTimeout(resolve, 100));
    expect(api.getTrips).not.toHaveBeenCalled();
    input.value = 'Synthetic';
    input.dispatchEvent(new Event('input'));
    await vi.waitFor(() =>
      expect(api.getTrips).toHaveBeenCalledWith(
        expect.objectContaining({ search: 'Synthetic', page: 1 }),
      ),
    );
    expect(api.getTrips).toHaveBeenCalledTimes(1);
    button('Clear filters').click();
    await fixture.whenStable();
    expect(fixture.componentInstance.query().search).toBeUndefined();
  });

  it('moves day/week scopes, shows Trip date only for multiple days and exposes only useful sorts', async () => {
    await settle();
    expect(fixture.nativeElement.querySelectorAll('th')).toHaveLength(5);
    const headers = Array.from(
      fixture.nativeElement.querySelectorAll('th') as NodeListOf<HTMLElement>,
    );
    expect(
      headers.find((header) => header.textContent?.includes('Route'))?.querySelector('button'),
    ).toBeNull();
    expect(
      headers.find((header) => header.textContent?.includes('Attention'))?.querySelector('button'),
    ).toBeNull();
    button('This week').click();
    await settle();
    expect(fixture.nativeElement.querySelectorAll('th')).toHaveLength(6);
    const start = new Date(fixture.componentInstance.serviceDate() + 'T12:00:00');
    fixture.nativeElement.querySelector('[aria-label="Next Trip Date scope"]').click();
    await settle();
    expect(
      new Date(fixture.componentInstance.serviceDate() + 'T12:00:00').getTime() - start.getTime(),
    ).toBe(7 * 86400000);
    const passengerSort = Array.from(
      fixture.nativeElement.querySelectorAll('th button') as NodeListOf<HTMLButtonElement>,
    ).find((item) => item.textContent?.includes('Passenger'))!;
    passengerSort.click();
    await settle();
    expect(api.getTrips).toHaveBeenLastCalledWith(
      expect.objectContaining({ sortBy: 'PassengerName', sortDirection: 'Ascending' }),
    );
    expect(passengerSort.closest('th')?.getAttribute('aria-sort')).toBe('ascending');
    button('Today').click();
    await settle();
    expect(fixture.nativeElement.querySelectorAll('th')).toHaveLength(5);
    expect(button('Today').getAttribute('aria-pressed')).toBe('true');
  });

  it('edits just one pickup, saves/cancels without opening details, and formats time and mobility', async () => {
    await settle();
    expect(fixture.nativeElement.textContent).toContain('10:00 AM');
    expect(fixture.nativeElement.textContent).toContain('Mobility unknown');
    expect(fixture.nativeElement.querySelector('input[type="time"]')).toBeNull();
    button('Set time').click();
    await fixture.whenStable();
    expect(fixture.componentInstance.selectedTrip()).toBeNull();
    button('Cancel').click();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('input[type="time"]')).toBeNull();
    button('Set time').click();
    await fixture.whenStable();
    const input = fixture.nativeElement.querySelector('input[type="time"]') as HTMLInputElement;
    input.value = '08:15';
    input.dispatchEvent(new Event('input'));
    api.getTrips.mockResolvedValue(response([{ ...trip, scheduledPickupTime: '08:15:00' }]));
    fixture.nativeElement
      .querySelector('form')
      .dispatchEvent(new Event('submit', { cancelable: true }));
    await vi.waitFor(() =>
      expect(api.setScheduledPickupTime).toHaveBeenCalledWith(trip.id, '08:15'),
    );
    await settle();
    expect(fixture.nativeElement.querySelector('input[type="time"]')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('8:15 AM');
    expect(fixture.componentInstance.selectedTrip()).toBeNull();
    fixture.nativeElement.querySelector('tbody tr').click();
    await fixture.whenStable();
    expect(fixture.componentInstance.selectedTrip()?.id).toBe(trip.id);
  });

  it('switches to a compact list without duplicating rows or fetching again', async () => {
    await settle();
    const calls = api.getTrips.mock.calls.length;
    viewport.next({ matches: false, breakpoints: {} });
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('table')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('ul[aria-label="Trips"] li')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('Mobility unknown');
    expect(fixture.nativeElement.textContent).toContain('10:00 AM');
    expect(api.getTrips).toHaveBeenCalledTimes(calls);
    button('Set time').click();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelector('input[type="time"]')).not.toBeNull();
    expect(fixture.componentInstance.selectedTrip()).toBeNull();
  });

  it('distinguishes filtered empty results and counts attention beyond the visible page', async () => {
    api.getTrips.mockResolvedValue({
      ...response(),
      scopeCount: 137,
      attentionCount: 60,
      totalCount: 137,
      totalPages: 3,
    });
    await settle();
    expect(fixture.nativeElement.textContent).toContain('60 need attention');
    expect(fixture.nativeElement.textContent).toContain('1–50 of 137');
    api.getTrips.mockResolvedValue(response([]));
    button('Needs attention').click();
    await settle();
    expect(fixture.nativeElement.textContent).toContain('No trips match these filters.');
  });
});

function deferred<T>() {
  let resolve!: (value: T) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<T>((res, rej) => {
    resolve = res;
    reject = rej;
  });
  return { promise, resolve, reject };
}
