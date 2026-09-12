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

  it('lists the unified user management rows', async () => {
    const listing = api.list();
    const request = http.expectOne('/api/users');

    expect(request.request.method).toBe('GET');
    request.flush([
      {
        id: '0199-3ee7-7bb9-75a8-96d12019cc91',
        type: 'User',
        firstName: 'Taylor',
        lastName: 'Example',
        displayName: 'Taylor Example',
        email: 'taylor@example.test',
        roles: ['Driver'],
        status: 'Active',
        expiresAt: null,
      },
      {
        id: '0199-3ee7-cf44-75ac-ae82f8501c42',
        type: 'Invitation',
        firstName: 'Jordan',
        lastName: 'Example',
        displayName: 'Jordan Example',
        email: 'jordan@example.test',
        roles: ['Dispatcher'],
        status: 'Invited',
        expiresAt: '2030-09-12T12:00:00Z',
      },
    ]);

    await expect(listing).resolves.toHaveLength(2);
  });

  it('uses the supported invite, update, and cancel endpoints', async () => {
    const details = {
      firstName: 'Taylor',
      lastName: 'Example',
      displayName: 'Taylor Example',
      email: 'taylor@example.test',
      roles: ['Driver'] as const,
      isActive: false,
    };

    const inviting = api.invite({ ...details, roles: [...details.roles] });
    const invitation = http.expectOne('/api/users/invitations');
    expect(invitation.request.method).toBe('POST');
    expect(invitation.request.body).toEqual({
      firstName: 'Taylor',
      lastName: 'Example',
      email: 'taylor@example.test',
      roles: ['Driver'],
    });
    invitation.flush(null);
    await inviting;

    const updating = api.update(
      {
        id: '0199-3ee7-7bb9-75a8-96d12019cc91',
        type: 'User',
        ...details,
        roles: [...details.roles],
        status: 'Active',
        expiresAt: null,
      },
      { ...details, roles: [...details.roles] },
    );
    const update = http.expectOne('/api/users/0199-3ee7-7bb9-75a8-96d12019cc91');
    expect(update.request.method).toBe('PUT');
    expect(update.request.body).toEqual({
      displayName: 'Taylor Example',
      roles: ['Driver'],
      isActive: false,
    });
    update.flush(null);
    await updating;

    const cancelling = api.cancelInvitation('synthetic-invitation');
    const cancellation = http.expectOne('/api/users/invitations/synthetic-invitation');
    expect(cancellation.request.method).toBe('DELETE');
    cancellation.flush(null);
    await cancelling;

    const resending = api.resendInvitation('synthetic-invitation');
    const resend = http.expectOne('/api/users/invitations/synthetic-invitation/resend');
    expect(resend.request.method).toBe('POST');
    expect(resend.request.body).toEqual({});
    resend.flush(null);
    await resending;
  });

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
