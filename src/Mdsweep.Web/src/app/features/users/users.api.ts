import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '@app/core/api/api-client';

export type UserRole = 'Administrator' | 'Dispatcher' | 'Driver';
export type UserDetails = {
  firstName: string;
  lastName: string;
  email: string;
  roles: UserRole[];
  isActive: boolean;
};
export type ManagedUser = Omit<UserDetails, 'email'> & {
  id: string;
  email: string | null;
  version: number;
};
export type Invitation = Omit<UserDetails, 'isActive'> & {
  id: string;
  status: 'Pending' | 'Expired' | 'Accepted' | 'Revoked';
  expiresAt: string;
  sentAt: string | null;
  deliveryError: string | null;
  version: number;
};
export type AccessHistory = {
  actorSubject: string;
  actorName: string;
  action: string;
  occurredAt: string;
  details: string | null;
};
export type UserManagement = {
  users: ManagedUser[];
  invitations: Invitation[];
  isAdministrator: boolean;
};
export type PendingInvitation = {
  id: string;
  tenantName: string;
  roles: UserRole[];
  expiresAt: string;
};

@Service()
export class UsersApi {
  readonly #api = inject(ApiClient);
  list(): Promise<UserManagement> {
    return firstValueFrom(this.#api.http.get<UserManagement>(this.#api.url('users')));
  }
  invite(details: UserDetails): Promise<Invitation> {
    return firstValueFrom(
      this.#api.http.post<Invitation>(this.#api.url('users/invitations'), details),
    );
  }
  update(user: ManagedUser, details: UserDetails): Promise<void> {
    return firstValueFrom(
      this.#api.http.put<void>(this.#api.url(`users/${user.id}`), {
        ...details,
        version: user.version,
      }),
    );
  }
  resend(id: string): Promise<Invitation> {
    return firstValueFrom(
      this.#api.http.post<Invitation>(this.#api.url(`users/invitations/${id}/resend`), {}),
    );
  }
  revoke(id: string): Promise<void> {
    return firstValueFrom(
      this.#api.http.post<void>(this.#api.url(`users/invitations/${id}/revoke`), {}),
    );
  }
  resetPassword(id: string): Promise<void> {
    return firstValueFrom(
      this.#api.http.post<void>(this.#api.url(`users/${id}/password-reset`), {}),
    );
  }
  history(id: string, invitation: boolean): Promise<AccessHistory[]> {
    return firstValueFrom(
      this.#api.http.get<AccessHistory[]>(
        this.#api.url(`users/${invitation ? 'invitations/' : ''}${id}/history`),
      ),
    );
  }
  pendingInvitation(): Promise<PendingInvitation[]> {
    return firstValueFrom(this.#api.http.get<PendingInvitation[]>(this.#api.url('invitation')));
  }
  async accept(id: string): Promise<void> {
    await firstValueFrom(this.#api.http.get(this.#api.url('auth/antiforgery')));
    await firstValueFrom(this.#api.http.post<void>(this.#api.url(`invitation/${id}/accept`), {}));
  }
}
