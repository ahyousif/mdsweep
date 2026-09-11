import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { applicationErrorInterceptor } from '../errors/application-error.interceptor';
import { AuthSessionService } from './auth-session.service';

describe('AuthSessionService', () => {
  let service: AuthSessionService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([applicationErrorInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(AuthSessionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('bootstraps a single-tenant session with one request', async () => {
    const establishing = service.establish();

    const session = http.expectOne('/api/auth/session');
    expect(session.request.method).toBe('GET');
    session.flush({
      userId: 'd449d57a-8f51-4a2a-9624-d6d474aaa6e7',
      displayName: 'Synthetic Dispatcher',
      email: 'dispatcher@example.test',
      activeTenant: {
        id: 'acme-transport',
        name: 'Acme Transport',
        roles: ['Dispatcher', 'Administrator'],
      },
      availableTenants: [],
    });

    await expect(establishing).resolves.toEqual({
      userId: 'd449d57a-8f51-4a2a-9624-d6d474aaa6e7',
      displayName: 'Synthetic Dispatcher',
      email: 'dispatcher@example.test',
      activeTenant: {
        id: 'acme-transport',
        name: 'Acme Transport',
        roles: ['Dispatcher', 'Administrator'],
      },
      availableTenants: [],
    });
  });

  it('allows a new invitee to bootstrap without a local User or Tenant', async () => {
    const establishing = service.establish();
    const session = {
      userId: null,
      displayName: '',
      email: 'invitee@example.test',
      activeTenant: null,
      availableTenants: [],
    };
    http.expectOne('/api/auth/session').flush(session);
    await expect(establishing).resolves.toEqual(session);
    expect(service.toTenantSession(session)).toBeNull();
    http.expectNone('/api/auth/tenant-context');
  });

  it('activates the first accepted membership and refreshes the session', async () => {
    const tenant = { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Driver'] };
    const session = {
      userId: 'user',
      displayName: 'Synthetic User',
      email: 'user@example.test',
      activeTenant: null,
      availableTenants: [tenant],
    };
    const establishing = service.establish();
    http.expectOne('/api/auth/session').flush(session);
    await Promise.resolve();
    http.expectOne('/api/auth/tenant-context').flush(null);
    await Promise.resolve();
    await Promise.resolve();
    http.expectOne('/api/auth/session').flush({ ...session, activeTenant: tenant });
    await expect(establishing).resolves.toMatchObject({ activeTenant: tenant });
  });

  it('uses the server Tenant selection instead of a stale browser preference', async () => {
    localStorage.setItem('mdsweep.tenant', 'abcd-efgh-jkmn');
    const tenant = { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Driver'] };
    const establishing = service.establish();
    http
      .expectOne('/api/auth/session')
      .flush({
        userId: 'user',
        displayName: 'Synthetic User',
        email: 'user@example.test',
        activeTenant: tenant,
        availableTenants: [tenant],
      });
    await expect(establishing).resolves.toMatchObject({ activeTenant: tenant });
    http.expectNone('/api/auth/tenant-context');
  });

  it('lists multiple Tenants without changing the active Tenant', async () => {
    const listing = service.availableSessions();
    const tenants = [
      { id: 'mdsw-eep2-3456', name: 'Alpha', roles: ['Administrator', 'Driver'] },
      { id: 'abcd-efgh-jkmn', name: 'Beta', roles: ['Dispatcher'] },
    ];
    http
      .expectOne('/api/auth/session')
      .flush({
        userId: 'user',
        displayName: 'Synthetic User',
        email: 'user@example.test',
        activeTenant: tenants[0],
        availableTenants: tenants,
      });
    await expect(listing).resolves.toMatchObject([
      { tenantId: tenants[0].id, roles: tenants[0].roles },
      { tenantId: tenants[1].id, roles: tenants[1].roles },
    ]);
    http.expectNone('/api/auth/tenant-context');
  });

  it('starts BFF sign-in when session bootstrap returns 401', async () => {
    const signIn = vi.spyOn(service, 'signIn').mockImplementation(() => undefined);
    const establishing = service.establish();

    http.expectOne('/api/auth/session').flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(establishing).rejects.toMatchObject({ status: 401 });
    expect(signIn).toHaveBeenCalledOnce();
  });

  it('selects a tenant only when the session requires it', async () => {
    const selecting = service.selectTenant('contoso-transport');

    const request = http.expectOne('/api/auth/tenant-context');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ tenantId: 'contoso-transport' });
    request.flush(null);

    await expect(selecting).resolves.toBeUndefined();
  });

  it('starts OIDC logout with the session bootstrap antiforgery token', () => {
    const submit = vi
      .spyOn(HTMLFormElement.prototype, 'submit')
      .mockImplementation(() => undefined);
    document.cookie = 'XSRF-TOKEN=sign-out-token; path=/';

    service.signOut();

    const form = submit.mock.instances[0] as HTMLFormElement;
    expect(form.getAttribute('method')).toBe('post');
    expect(form.getAttribute('action')).toBe('/api/auth/logout');
    expect(
      form.querySelector('input[name="__RequestVerificationToken"]')?.getAttribute('value'),
    ).toBe('sign-out-token');

    form.remove();
    submit.mockRestore();
  });

  it('preserves a safe invitation return URL when switching accounts', () => {
    const submit = vi
      .spyOn(HTMLFormElement.prototype, 'submit')
      .mockImplementation(() => undefined);
    document.cookie = 'XSRF-TOKEN=sign-out-token; path=/';

    service.signOut('/invitations/accept?token=ABC');

    const form = submit.mock.instances[0] as HTMLFormElement;
    expect(form.getAttribute('action')).toBe(
      '/api/auth/logout?returnUrl=%2Finvitations%2Faccept%3Ftoken%3DABC',
    );

    form.remove();
    submit.mockRestore();
  });
});
