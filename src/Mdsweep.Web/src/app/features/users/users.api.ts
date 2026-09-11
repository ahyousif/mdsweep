import { inject, Service } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiClient } from '@app/core/api/api-client';

export type UserRole = 'Administrator' | 'Dispatcher' | 'Driver';

export type UserDetails = {
  displayName: string;
  firstName: string;
  lastName: string;
  email: string;
  roles: UserRole[];
  isActive: boolean;
};

export type UserListItem = {
  id: string;
  type: 'User' | 'Invitation';
  firstName: string;
  lastName: string;
  email: string;
  displayName: string;
  roles: UserRole[];
  status: 'Active' | 'Inactive' | 'Invited';
  expiresAt: string | null;
};

@Service()
export class UsersApi {
  readonly #api = inject(ApiClient);

  list(): Promise<UserListItem[]> {
    return firstValueFrom(this.#api.http.get<UserListItem[]>(this.#api.url('users')));
  }

  invite(details: UserDetails): Promise<void> {
    return firstValueFrom(
      this.#api.http.post<void>(this.#api.url('users/invitations'), {
        firstName: details.firstName,
        lastName: details.lastName,
        email: details.email,
        roles: details.roles,
      }),
    );
  }

  update(user: UserListItem, details: UserDetails): Promise<void> {
    return firstValueFrom(
      this.#api.http.put<void>(this.#api.url(`users/${user.id}`), {
        displayName: details.displayName,
        roles: details.roles,
        isActive: details.isActive,
      }),
    );
  }

  cancelInvitation(id: string): Promise<void> {
    return firstValueFrom(
      this.#api.http.delete<void>(this.#api.url(`users/invitations/${id}`)),
    );
  }

  resendInvitation(id: string): Promise<void> {
    return firstValueFrom(
      this.#api.http.post<void>(this.#api.url(`users/invitations/${id}/resend`), {}),
    );
  }

  async accept(token: string): Promise<void> {
    await firstValueFrom(this.#api.http.get(this.#api.url('auth/antiforgery')));
    await firstValueFrom(
      this.#api.http.post<void>(this.#api.url('users/invitations/accept'), { token }),
    );
  }
}
