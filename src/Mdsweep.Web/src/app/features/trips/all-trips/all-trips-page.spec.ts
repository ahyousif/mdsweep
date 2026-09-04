import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { ApplicationError } from '@app/core/errors/application-error';
import { AllTripsApi } from './all-trips.api';
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
        provideTanStackQuery(new QueryClient({ defaultOptions: { queries: { retry: false } } })),
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
      expect(api.getTrips).toHaveBeenCalledWith(expect.objectContaining({ startDate: '2026-09-03', endDate: '2026-09-03' }));
      expect(fixture.nativeElement.textContent).toContain('Trips could not be loaded');
      expect(fixture.nativeElement.textContent).toContain('Trips are temporarily unavailable.');
    });

    fixture.componentInstance.goToToday();
    expect(fixture.componentInstance.serviceDate()).toBe(today);
  });
});
