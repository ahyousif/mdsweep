import { provideLocalization } from '@app/core/i18n/localization.providers';
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
        provideLocalization(),
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
            getAllTrips: () => Promise.resolve([]),
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
    await harness.fixture.whenStable();

    expect(router.url).toBe('/trips');
    expect(harness.routeNativeElement?.textContent).toContain('Trips');
    const page = harness.routeNativeElement as HTMLElement;
    await vi.waitFor(() => {
      harness.detectChanges();
      expect(page.querySelector('[data-slot="empty"]')).not.toBeNull();
    });
    expect(page.querySelector('[data-journey-header]')).toBeNull();
    expect(page.querySelector('[data-slot="empty-title"]')?.textContent?.trim()).toBe(
      'No trips for this date',
    );
    expect(page.querySelector('[data-slot="empty-description"]')?.textContent?.trim()).toBe(
      'Import a manifest or add a trip to get started.',
    );
    const toolbarButtons = Array.from(page.querySelectorAll('app-trip-toolbar button'));
    expect(toolbarButtons.some((button) => button.textContent?.trim() === 'Import trips')).toBe(true);
    expect(toolbarButtons.some((button) => button.textContent?.trim() === 'Add trip')).toBe(true);
  });
});
