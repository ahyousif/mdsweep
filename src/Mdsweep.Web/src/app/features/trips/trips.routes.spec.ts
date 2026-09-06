import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { AllTripsApi } from './all-trips/all-trips.api';
import { TripImportApi } from './trip-import/trip-import.api';
import { routes } from '@app/app.routes';

describe('Trips routes', () => {
  it('preserves the Trip Import route and can return to All Trips', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(new QueryClient()),
        provideRouter(routes),
        {
          provide: AllTripsApi,
          useValue: { getTrips: () => Promise.resolve({ items: [] }), setScheduledPickupTime: () => Promise.resolve() },
        },
        { provide: TripImportApi, useValue: { import: () => Promise.resolve() } },
      ],
    });
    const harness = await RouterTestingHarness.create();
    const router = TestBed.inject(Router);

    await harness.navigateByUrl('/trips/import');
    expect(router.url).toBe('/trips/import');

    await harness.navigateByUrl('/trips');
    expect(router.url).toBe('/trips');
  });
});
