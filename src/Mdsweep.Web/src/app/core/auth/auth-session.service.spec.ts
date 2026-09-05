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
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([applicationErrorInterceptor])), provideHttpClientTesting()],
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
      activeTenant: {
        id: 'acme-transport',
        name: 'Acme Transport',
        roles: ['Dispatcher', 'Administrator'],
      },
      availableTenants: [],
    });
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
    const submit = vi.spyOn(HTMLFormElement.prototype, 'submit').mockImplementation(() => undefined);
    document.cookie = 'XSRF-TOKEN=sign-out-token; path=/';

    service.signOut();

    const form = submit.mock.instances[0] as HTMLFormElement;
    expect(form.getAttribute('method')).toBe('post');
    expect(form.getAttribute('action')).toBe('/api/auth/logout');
    expect(form.querySelector('input[name="__RequestVerificationToken"]')?.getAttribute('value')).toBe(
      'sign-out-token',
    );

    form.remove();
    submit.mockRestore();
  });
});
