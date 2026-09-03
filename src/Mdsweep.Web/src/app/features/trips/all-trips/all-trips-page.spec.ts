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

  it('uses the selected service date as a filter and shows a query failure separately', async () => {
    api.getTrips.mockRejectedValue(new ApplicationError('Trips are temporarily unavailable.', 503));
    fixture.detectChanges();

    const filter = fixture.nativeElement.querySelector('#service-date-filter') as HTMLInputElement;
    filter.value = '2026-09-02';
    filter.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(api.getTrips).toHaveBeenCalledWith('2026-09-02');
      expect(fixture.nativeElement.textContent).toContain('Trips could not be loaded');
      expect(fixture.nativeElement.textContent).toContain('Trips are temporarily unavailable.');
    });
  });
});
