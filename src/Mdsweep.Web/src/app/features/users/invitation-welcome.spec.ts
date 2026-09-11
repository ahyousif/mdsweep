import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { ApplicationError } from '@app/core/errors/application-error';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { UsersApi } from './users.api';
import { InvitationWelcome } from './invitation-welcome';

describe('InvitationWelcome', () => {
  let fixture: ComponentFixture<InvitationWelcome>;
  const accept = vi.fn();
  const signOut = vi.fn();

  beforeEach(async () => {
    window.history.replaceState({}, '', '/invitations/accept?token=ABC');
    accept.mockReset();
    signOut.mockReset();
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(
          new QueryClient({ defaultOptions: { queries: { retry: false } } }),
        ),
        {
          provide: AuthSessionService,
          useValue: { availableSessions: () => Promise.resolve([]), signOut },
        },
        { provide: UsersApi, useValue: { accept } },
      ],
    });
    fixture = TestBed.createComponent(InvitationWelcome);
    fixture.componentRef.setInput('token', 'ABC');
    fixture.componentRef.setInput('currentEmail', 'existing-user@example.com');
    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => window.history.replaceState({}, '', '/'));

  it('shows account-switch recovery for a rejected email mismatch', async () => {
    accept.mockRejectedValue(
      new ApplicationError('Sign in with the invited email.', 400, undefined, {
        invitationEmailMismatch: ['Sign in with the invited email.'],
      }),
    );

    const acceptButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Accept invitation'))!;
    acceptButton.click();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain(
        'This invitation belongs to a different account.',
      );
    });
    expect(fixture.nativeElement.textContent).toContain('existing-user@example.com');
    expect(fixture.nativeElement.textContent).toContain('Continue with another account');
  });

  it('preserves the invitation URL when continuing with another account', async () => {
    accept.mockRejectedValue(
      new ApplicationError('Sign in with the invited email.', 400, undefined, {
        invitationEmailMismatch: ['Sign in with the invited email.'],
      }),
    );
    const buttons = () =>
      Array.from(
        fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
      );
    buttons()
      .find((button) => button.textContent?.includes('Accept invitation'))!
      .click();
    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain('Continue with another account');
    });

    buttons()
      .find((button) => button.textContent?.includes('Continue with another account'))!
      .click();

    expect(signOut).toHaveBeenCalledWith('/invitations/accept?token=ABC');
  });
});
