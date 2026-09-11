import { ComponentFixture, TestBed } from '@angular/core/testing';
import { AuthSessionService } from '@app/core/auth/auth-session.service';
import { ApplicationError } from '@app/core/errors/application-error';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import InvitationWelcome from './invitation-welcome';
import { UsersApi } from './users.api';

describe('InvitationWelcome', () => {
  let fixture: ComponentFixture<InvitationWelcome>;
  const accept = vi.fn();
  const signOut = vi.fn();
  const establish = vi.fn();
  const toTenantSession = vi.fn();

  beforeEach(async () => {
    window.history.replaceState({}, '', '/invitations/accept?token=ABC');
    accept.mockReset();
    signOut.mockReset();
    establish.mockReset();
    toTenantSession.mockReset();
    toTenantSession.mockReturnValue({ tenantId: 'mdsw-eep2-3456' });
    TestBed.configureTestingModule({
      providers: [
        provideTanStackQuery(new QueryClient({ defaultOptions: { queries: { retry: false } } })),
        {
          provide: AuthSessionService,
          useValue: {
            availableSessions: () => Promise.resolve([]),
            establish,
            signOut,
            toTenantSession,
          },
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
      Array.from(fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>);
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

  it('refreshes tenant access before completing invitation acceptance', async () => {
    accept.mockResolvedValue(undefined);
    establish.mockResolvedValue({
      userId: 'accepted-user',
      displayName: 'Accepted User',
      email: 'existing-user@example.com',
      activeTenant: {
        id: 'mdsw-eep2-3456',
        name: 'Synthetic Tenant',
        roles: ['Driver'],
      },
      availableTenants: [
        {
          id: 'mdsw-eep2-3456',
          name: 'Synthetic Tenant',
          roles: ['Driver'],
        },
      ],
    });
    const accepted = vi.fn();
    fixture.componentInstance.accepted.subscribe(accepted);

    const acceptButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Accept invitation'))!;
    acceptButton.click();

    await vi.waitFor(() => expect(establish).toHaveBeenCalledOnce());
    expect(accepted).toHaveBeenCalledOnce();
    expect(establish.mock.invocationCallOrder[0]).toBeLessThan(
      accepted.mock.invocationCallOrder[0],
    );
  });

  it('confirms acceptance and offers Tenant selection when no Tenant is active', async () => {
    accept.mockResolvedValue(undefined);
    toTenantSession.mockReturnValue(null);
    establish.mockResolvedValue({
      userId: 'accepted-user',
      displayName: 'Accepted User',
      email: 'existing-user@example.com',
      activeTenant: null,
      availableTenants: [
        { id: 'first-tenant', name: 'First Tenant', roles: ['Driver'] },
        { id: 'invited-tenant', name: 'Invited Tenant', roles: ['Dispatcher'] },
      ],
    });

    const acceptButton = Array.from(
      fixture.nativeElement.querySelectorAll('button') as NodeListOf<HTMLButtonElement>,
    ).find((button) => button.textContent?.includes('Accept invitation'))!;
    acceptButton.click();

    await vi.waitFor(() => {
      fixture.detectChanges();
      expect(fixture.nativeElement.textContent).toContain(
        'Invitation accepted. Choose the Tenant you want to open.',
      );
    });
    expect(fixture.nativeElement.textContent).not.toContain('Try accepting the invitation again.');
    expect(fixture.nativeElement.textContent).not.toContain('Accept invitation');
  });
});
