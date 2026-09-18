import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PassengersApi } from './passengers.api';

describe('PassengersApi', () => {
  let api: PassengersApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(PassengersApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('uses the existing Passenger list, detail, and create endpoints', async () => {
    const listing = api.list({ search: 'Example', page: 2, pageSize: 25 });
    const listRequest = http.expectOne('/api/passengers?page=2&pageSize=25&search=Example');
    expect(listRequest.request.method).toBe('GET');
    listRequest.flush({ items: [], totalCount: 0, page: 2, pageSize: 25, totalPages: 0 });
    await expect(listing).resolves.toMatchObject({ page: 2 });

    const detail = api.get('synthetic-passenger');
    const detailRequest = http.expectOne('/api/passengers/synthetic-passenger');
    expect(detailRequest.request.method).toBe('GET');
    detailRequest.flush({ id: 'synthetic-passenger', firstName: 'Taylor', lastName: 'Example' });
    await expect(detail).resolves.toMatchObject({ id: 'synthetic-passenger' });

    const creating = api.create({ firstName: 'Taylor', lastName: 'Example', brokerMemberId: 'MEMBER-42' });
    const createRequest = http.expectOne('/api/passengers');
    expect(createRequest.request.method).toBe('POST');
    expect(createRequest.request.body).toEqual({ firstName: 'Taylor', lastName: 'Example', brokerMemberId: 'MEMBER-42' });
    createRequest.flush({ id: 'synthetic-passenger', firstName: 'Taylor', lastName: 'Example' });
    await expect(creating).resolves.toMatchObject({ id: 'synthetic-passenger' });
  });

  it('keeps status out of details PUT and uses explicit lifecycle endpoints', async () => {
    const details = {
      firstName: 'Taylor',
      lastName: 'Example',
      brokerMemberId: 'MEMBER-42',
      dateOfBirth: null,
      phoneNumber: null,
      alternatePhoneNumber: null,
      passengerType: null,
      specialNeeds: null,
      notes: null,
    };
    const updating = api.update('synthetic-passenger', details);
    const updateRequest = http.expectOne('/api/passengers/synthetic-passenger');
    expect(updateRequest.request.method).toBe('PUT');
    expect(updateRequest.request.body).toEqual(details);
    expect(updateRequest.request.body).not.toHaveProperty('isActive');
    updateRequest.flush(null);
    await updating;

    const disabling = api.disable('synthetic-passenger');
    const disableRequest = http.expectOne('/api/passengers/synthetic-passenger/disable');
    expect(disableRequest.request.method).toBe('POST');
    disableRequest.flush(null);
    await disabling;

    const enabling = api.enable('synthetic-passenger');
    const enableRequest = http.expectOne('/api/passengers/synthetic-passenger/enable');
    expect(enableRequest.request.method).toBe('POST');
    enableRequest.flush(null);
    await enabling;
  });
});
