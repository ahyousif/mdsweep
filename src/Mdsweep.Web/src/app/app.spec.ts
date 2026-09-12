import { provideLocalization } from '@app/core/i18n/localization.providers';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from './core/auth/auth-session.service';
import { ApplicationError } from './core/errors/application-error';
import { App } from './app';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  const signIn = vi.fn();

  afterEach(() => {
    window.history.replaceState({}, '', '/');
    vi.clearAllMocks();
  });

  beforeEach(() => {
    window.history.replaceState({}, '', '/invitations/accept?token=ABC');
    TestBed.configureTestingModule({
      providers: [
        provideLocalization(),
        provideTanStackQuery(new QueryClient()),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            establish: () => {
              signIn();
              return Promise.reject(new ApplicationError('Unauthenticated.', 401));
            },
            signIn,
          },
        },
      ],
    });
    fixture = TestBed.createComponent(App);
  });

  it('starts BFF sign-in without rendering the removed sign-in card', async () => {
    fixture.detectChanges();

    await vi.waitFor(() => {
      fixture.detectChanges();
      const page = fixture.nativeElement.textContent as string;
      expect(signIn).toHaveBeenCalledOnce();
      expect(page).not.toContain('Sign in');
      expect(window.location.pathname).toBe('/invitations/accept');
      expect(window.location.search).toBe('?token=ABC');
    });
  });

  it('keeps an authenticated invitee on the plural invitation URL with its token', async () => {
    window.history.replaceState({}, '', '/invitations/accept?token=ABC');
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideLocalization(),
        provideTanStackQuery(new QueryClient()),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            establish: () =>
              Promise.resolve({
                userId: 'existing-user',
                displayName: 'Existing User',
                email: 'existing-user@example.com',
                activeTenant: {
                  id: 'mdsw-eep2-3456',
                  name: 'Synthetic Tenant',
                  roles: ['Dispatcher'],
                },
                availableTenants: [
                  {
                    id: 'mdsw-eep2-3456',
                    name: 'Synthetic Tenant',
                    roles: ['Dispatcher'],
                  },
                ],
              }),
            toTenantSession: () => ({
              appUserId: 'existing-user',
              displayName: 'Existing User',
              tenantId: 'mdsw-eep2-3456',
              tenantName: 'Synthetic Tenant',
              roles: ['Dispatcher'],
            }),
            availableSessions: () => Promise.resolve([]),
          },
        },
      ],
    });
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    await vi.waitFor(() => {
      fixture.detectChanges();
      const page = fixture.nativeElement.textContent as string;
      expect(page).toContain('Accept invitation');
      expect(window.location.pathname).toBe('/invitations/accept');
      expect(window.location.search).toBe('?token=ABC');
    });
  });
});
