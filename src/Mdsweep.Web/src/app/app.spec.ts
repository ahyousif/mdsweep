import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from './core/auth/auth-session.service';
import { ApplicationError } from './core/errors/application-error';
import { App } from './app';

describe('App', () => {
  let fixture: ComponentFixture<App>;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(new QueryClient()),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            establish: () => Promise.reject(new ApplicationError('Unauthenticated.', 401)),
            signIn: () => undefined,
          },
        },
      ],
    });
    fixture = TestBed.createComponent(App);
  });

  it('presents a 401 as the normal sign-in state without a provider-selection error', async () => {
    fixture.detectChanges();

    await vi.waitFor(() => {
      fixture.detectChanges();
      const page = fixture.nativeElement.textContent as string;
      expect(page).toContain('Sign in');
      expect(page).not.toContain('Provider selection required');
    });
  });

  it('uses organization language for a session-establishment error', async () => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(new QueryClient()),
        provideRouter([]),
        {
          provide: AuthSessionService,
          useValue: {
            establish: () => Promise.reject(new Error('Choose an organization before using MDSweep.')),
            signIn: () => undefined,
          },
        },
      ],
    });
    fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Organization selection required');
    });
  });
});
