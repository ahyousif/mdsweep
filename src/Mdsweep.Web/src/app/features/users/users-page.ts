import { Component, computed, inject, signal } from '@angular/core';

import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectMutation, injectQuery, QueryClient } from '@tanstack/angular-query-experimental';

import { httpErrorMessage } from '@app/core/api/http-error-message';

import UserDetail from './user-detail/user-detail';
import { UserForm } from './user-form';
import UserList from './user-list/user-list';
import UserToolbar, {
  type UserStatusCounts,
  type UserStatusFilter,
} from './user-toolbar/user-toolbar';
import { UsersApi, type UserDetails, type UserListItem } from './users.api';
import { userQueryKeys, usersQueryOptions } from './users.queries';

type Action =
  | { kind: 'invite'; details: UserDetails }
  | { kind: 'update'; user: UserListItem; details: UserDetails }
  | { kind: 'resendInvitation'; user: UserListItem }
  | { kind: 'cancelInvitation'; user: UserListItem };

@Component({
  selector: 'app-users-page',
  imports: [
    HlmButton,
    HlmSpinner,
    UserDetail,
    UserForm,
    UserList,
    UserToolbar,
    ...HlmAlertImports,
    ...HlmDialogImports,
  ],
  templateUrl: './users-page.html',
  host: { class: 'block h-full min-h-0' },
})
export default class UsersPage {
  readonly #api = inject(UsersApi);
  readonly #queries = inject(QueryClient);

  readonly listing = injectQuery(() => usersQueryOptions(this.#api));
  readonly search = signal('');
  readonly activeFilter = signal<UserStatusFilter>('All');
  readonly selectedUserKey = signal<string | null>(null);
  readonly inviting = signal(false);
  readonly editing = signal(false);
  readonly message = signal('');
  readonly error = signal('');

  readonly statusCounts = computed<UserStatusCounts>(() => {
    const users = this.listing.data() ?? [];

    return {
      All: users.length,
      Active: users.filter((user) => user.status === 'Active').length,
      Invited: users.filter((user) => user.status === 'Invited').length,
      Disabled: users.filter((user) => user.status === 'Inactive').length,
    };
  });

  // TODO: revisit this mess
  readonly users = computed(() => {
    const search = this.search().trim().toLowerCase();
    const filter = this.activeFilter();

    return (this.listing.data() ?? []).filter((user) => {
      const matchesSearch =
        !search ||
        `${user.displayName} ${user.firstName} ${user.lastName} ${user.email}`
          .toLowerCase()
          .includes(search);

      const matchesFilter =
        filter === 'All' ||
        user.status === filter ||
        (filter === 'Disabled' && user.status === 'Inactive');

      return matchesSearch && matchesFilter;
    });
  });

  readonly selectedUser = computed<UserListItem | null>(() => {
    const selectedKey = this.selectedUserKey();

    return (this.listing.data() ?? []).find((user) => this.userKey(user) === selectedKey) ?? null;
  });

  //TODO: revisit this mess
  readonly mutation = injectMutation(() => ({
    mutationFn: (action: Action) => this.perform(action),
    onSuccess: async (_: void, action: Action) => {
      this.inviting.set(false);
      this.editing.set(false);
      this.message.set(
        action.kind === 'invite'
          ? `Invitation sent to ${action.details.email}`
          : action.kind === 'resendInvitation'
            ? `Invitation resent to ${action.user.email}`
            : action.kind === 'cancelInvitation'
              ? 'Invitation cancelled.'
              : 'User updated.',
      );

      if (action.kind === 'cancelInvitation') {
        this.selectedUserKey.set(null);
      }

      await this.#queries.invalidateQueries({ queryKey: userQueryKeys.all });

      if (action.kind === 'update') {
        await this.#queries.invalidateQueries({ queryKey: ['auth', 'session'] });
      }
    },
    onError: async (error: unknown) => {
      this.error.set(
        httpErrorMessage(error, 'The change could not be saved. Refresh and try again.'),
      );
      await this.#queries.invalidateQueries({ queryKey: userQueryKeys.all });
    },
  }));

  loadError(): string {
    return httpErrorMessage(this.listing.error(), 'Users could not be loaded. Try again.');
  }

  setSearch(value: string): void {
    this.search.set(value);
  }

  setFilter(filter: UserStatusFilter): void {
    this.activeFilter.set(filter);
  }

  startInvite(): void {
    this.editing.set(false);
    this.inviting.set(true);
    this.clearFeedback();
  }

  selectUser(user: UserListItem): void {
    this.selectedUserKey.set(this.userKey(user));
    this.editing.set(false);
    this.clearFeedback();
  }

  closeUserDetail(): void {
    this.selectedUserKey.set(null);
    this.editing.set(false);
  }

  save(details: UserDetails): void {
    const user = this.selectedUser();

    this.run(
      user && this.editing() ? { kind: 'update', user, details } : { kind: 'invite', details },
    );
  }

  changeAccess(isActive: boolean): void {
    const user = this.selectedUser();

    if (!user || user.type !== 'User') {
      return;
    }

    this.run({
      kind: 'update',
      user,
      details: {
        displayName: user.displayName,
        firstName: user.firstName,
        lastName: user.lastName,
        email: user.email,
        roles: user.roles,
        isActive,
      },
    });
  }

  cancelInvitation(): void {
    const user = this.selectedUser();

    if (user?.type === 'Invitation') {
      this.run({ kind: 'cancelInvitation', user });
    }
  }

  resendInvitation(): void {
    const user = this.selectedUser();

    if (user?.type === 'Invitation') {
      this.run({ kind: 'resendInvitation', user });
    }
  }

  private run(action: Action): void {
    if (this.mutation.isPending()) {
      return;
    }

    this.clearFeedback();
    this.mutation.mutate(action);
  }

  private userKey(user: UserListItem): string {
    return `${user.type}:${user.id}`;
  }

  private clearFeedback(): void {
    this.error.set('');
    this.message.set('');
  }

  private perform(action: Action): Promise<void> {
    switch (action.kind) {
      case 'invite':
        return this.#api.invite(action.details);
      case 'update':
        return this.#api.update(action.user, action.details);
      case 'resendInvitation':
        return this.#api.resendInvitation(action.user.id);
      case 'cancelInvitation':
        return this.#api.cancelInvitation(action.user.id);
    }
  }
}
