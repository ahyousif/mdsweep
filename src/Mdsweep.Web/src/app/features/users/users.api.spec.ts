import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { UsersApi } from './users.api';

describe('UsersApi', () => {
  let api: UsersApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    api = TestBed.inject(UsersApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('accepts an invitation through the Users invitation endpoint', async () => {
    const accepting = api.accept('ABC');

    const antiforgery = http.expectOne('/api/auth/antiforgery');
    expect(antiforgery.request.method).toBe('GET');
    antiforgery.flush({ token: 'synthetic-antiforgery-token' });
    await Promise.resolve();

    const acceptance = http.expectOne('/api/users/invitations/accept');
    expect(acceptance.request.method).toBe('POST');
    expect(acceptance.request.body).toEqual({ token: 'ABC' });
    acceptance.flush(null);

    await expect(accepting).resolves.toBeUndefined();
  });
});
