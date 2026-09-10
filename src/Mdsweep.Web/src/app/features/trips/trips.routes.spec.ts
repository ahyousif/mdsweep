import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { HlmDialogService } from '@spartan-ng/helm/dialog';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';

import { routes } from '@app/app.routes';

import { TripsApi } from './trips.api';

describe('Trips routes', () => {
  it('loads the trips page', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideTanStackQuery(
          new QueryClient({
            defaultOptions: {
              queries: {
                retry: false,
              },
            },
          }),
        ),
        {
          provide: TripsApi,
          useValue: {
            getTrips: () =>
              Promise.resolve({
                items: [],
                totalCount: 0,
                page: 1,
                pageSize: 100,
                totalPages: 0,
              }),
          },
        },
        {
          provide: HlmDialogService,
          useValue: {
            open: () => {
              throw new Error('Dialog should not open during route test.');
            },
          },
        },
      ],
    });

    const harness = await RouterTestingHarness.create();
    const router = TestBed.inject(Router);

    await harness.navigateByUrl('/trips');

    expect(router.url).toBe('/trips');
    expect(harness.routeNativeElement?.textContent).toContain('Trips');
  });
});
