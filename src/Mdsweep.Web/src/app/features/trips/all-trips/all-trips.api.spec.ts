import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AllTripsApi } from './all-trips.api';

describe('AllTripsApi', () => {
  let api: AllTripsApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(AllTripsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('requests the paged Trips endpoint with service-date and paging parameters', async () => {
    const request = api.getTrips({ startDate: '2026-09-02', endDate: '2026-09-02' });
    const call = http.expectOne('/api/trips?startDate=2026-09-02&endDate=2026-09-02&page=1&pageSize=50&sortBy=ScheduledPickupTime&sortDirection=Ascending');
    expect(call.request.method).toBe('GET');
    call.flush({ items: [], totalCount: 0, page: 1, pageSize: 50, totalPages: 0 });
    await expect(request).resolves.toMatchObject({ totalCount: 0 });
  });

  it('uses the Trip GUID when saving a scheduled pickup time', async () => {
    const request = api.setScheduledPickupTime('3b4f9230-7c8a-4b6b-a0af-1d2c3e4f5a6b', '09:15');
    const call = http.expectOne('/api/trips/3b4f9230-7c8a-4b6b-a0af-1d2c3e4f5a6b/scheduled-pickup-time');
    expect(call.request.method).toBe('PUT');
    expect(call.request.body).toEqual({ scheduledPickupTime: '09:15:00' });
    call.flush(null);
    await expect(request).resolves.toBeNull();
  });
});
