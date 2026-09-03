import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthSessionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('establishes the session after fetching antiforgery before and after tenant selection', async () => {
    const establishing = service.establish();

    const currentUser = http.expectOne('/api/auth/me');
    expect(currentUser.request.method).toBe('GET');
    currentUser.flush([
      { userId: 'd449d57a-8f51-4a2a-9624-d6d474aaa6e7', tenantId: 'acme-transport', role: 'Dispatcher' },
      {
        userId: 'd449d57a-8f51-4a2a-9624-d6d474aaa6e7',
        tenantId: 'acme-transport',
        role: 'Administrator',
      },
    ]);
    await Promise.resolve();

    const initialAntiforgery = http.expectOne('/api/auth/antiforgery');
    expect(initialAntiforgery.request.method).toBe('GET');
    initialAntiforgery.flush({ token: 'initial' });
    await Promise.resolve();

    const selectTenant = http.expectOne('/api/auth/tenant-context');
    expect(selectTenant.request.method).toBe('POST');
    expect(selectTenant.request.body).toEqual({ tenantId: 'acme-transport' });
    selectTenant.flush(null);
    await Promise.resolve();

    const refreshedAntiforgery = http.expectOne('/api/auth/antiforgery');
    expect(refreshedAntiforgery.request.method).toBe('GET');
    refreshedAntiforgery.flush({ token: 'refreshed' });

    await expect(establishing).resolves.toEqual({
      appUserId: 'd449d57a-8f51-4a2a-9624-d6d474aaa6e7',
      providerId: 'acme-transport',
      roles: ['Dispatcher', 'Administrator'],
    });
  });
});
