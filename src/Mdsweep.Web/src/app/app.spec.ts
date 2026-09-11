import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { AuthSessionService } from './core/auth/auth-session.service';
import { ApplicationError } from './core/errors/application-error';
import { App } from './app';

describe('App', () => {
  let fixture: ComponentFixture<App>;
  const signIn = vi.fn();

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
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
    });
  });
});
