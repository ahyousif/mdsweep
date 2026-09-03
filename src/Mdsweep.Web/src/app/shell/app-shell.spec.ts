import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { ApplicationError } from '@app/core/errors/application-error';
import { AppShell } from './app-shell';

describe('AppShell', () => {
  let fixture: ComponentFixture<AppShell>;

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
    fixture = TestBed.createComponent(AppShell);
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
});
