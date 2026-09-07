import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmEmptyImports } from '@spartan-ng/helm/empty';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmSeparator } from '@spartan-ng/helm/separator';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmH3, HlmMuted, HlmSmall } from '@spartan-ng/helm/typography';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmTabsImports } from '@spartan-ng/helm/tabs';
import { HlmTableImports } from '@spartan-ng/helm/table';
import { injectMutation, injectQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { UserForm } from './user-form';
import { UsersApi, type Invitation, type ManagedUser, type UserDetails } from './users.api';
import { userQueryKeys, usersQueryOptions } from './users.queries';

type Action =
  | { kind: 'invite'; details: UserDetails }
  | { kind: 'update'; user: ManagedUser; details: UserDetails }
  | { kind: 'resend' | 'revoke' | 'reset'; id: string };

@Component({
  selector: 'app-users-page',
  imports: [
    DatePipe,
    UserForm,
    HlmButton,
    HlmInput,
    HlmBadge,
    HlmH3,
    HlmMuted,
    HlmSmall,
    HlmSeparator,
    HlmSpinner,
    HlmEmptyImports,
    HlmFieldImports,
    ...HlmAlertImports,
    ...HlmCardImports,
    ...HlmDialogImports,
    ...HlmTableImports,
    HlmTabsImports,
  ],
  templateUrl: './users-page.html',
})
export default class UsersPage {
  readonly #api = inject(UsersApi);
  readonly #queries = inject(QueryClient);
  readonly listing = injectQuery(() => usersQueryOptions(this.#api));
  readonly search = signal('');
  readonly activeTab = signal('users');
  readonly inviting = signal(false);
  readonly editing = signal<ManagedUser | null>(null);
  readonly message = signal('');
  readonly error = signal('');
  readonly selectedHistory = signal<{ id: string; invitation: boolean; name: string } | null>(null);
  readonly visibleHistory = computed(() => {
    const selected = this.selectedHistory();
    return selected?.invitation === (this.activeTab() === 'invitations') ? selected : null;
  });
  readonly editingValue = computed(() => {
    const user = this.editing();
    return user ? { ...user, email: user.email ?? '' } : null;
  });
  readonly users = computed(() =>
    (this.listing.data()?.users ?? []).filter((x) => this.matches(x)),
  );
  readonly invitations = computed(() =>
    (this.listing.data()?.invitations ?? []).filter((x) => this.matches(x)),
  );
  readonly history = injectQuery(() => ({
    queryKey: userQueryKeys.history(
      this.selectedHistory()?.id ?? '',
      this.selectedHistory()?.invitation ?? false,
    ),
    queryFn: () =>
      this.#api.history(this.selectedHistory()!.id, this.selectedHistory()!.invitation),
    enabled: this.selectedHistory() !== null,
  }));
  readonly mutation = injectMutation(() => ({
    mutationFn: (action: Action) => this.perform(action),
    onSuccess: async (invitation: Invitation | void, action: Action) => {
      if (action.kind === 'invite') {
        this.activeTab.set('invitations');
        this.search.set('');
      }
      this.inviting.set(false);
      this.editing.set(null);
      this.message.set(
        invitation?.deliveryError
          ? 'Invitation saved. Email delivery failed; use Resend after email delivery is configured.'
          : action.kind === 'invite' || action.kind === 'resend'
            ? 'Invitation email sent.'
            : action.kind === 'reset'
              ? 'Password reset email sent.'
              : action.kind === 'revoke'
                ? 'Invitation revoked.'
                : 'User updated.',
      );
      await this.#queries.invalidateQueries({ queryKey: userQueryKeys.all });
      if (action.kind === 'update')
        await this.#queries.invalidateQueries({ queryKey: ['auth', 'session'] });
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
  historyError(): string {
    return httpErrorMessage(this.history.error(), 'History could not be loaded. Try again.');
  }
  startInvite(): void {
    this.editing.set(null);
    this.inviting.set(true);
    this.error.set('');
    this.message.set('');
  }
  edit(user: ManagedUser): void {
    this.inviting.set(false);
    this.editing.set(user);
    this.error.set('');
    this.message.set('');
  }
  run(action: Action): void {
    if (this.mutation.isPending()) return;
    this.error.set('');
    this.message.set('');
    this.mutation.mutate(action);
  }
  save(details: UserDetails): void {
    const user = this.editing();
    this.run(user ? { kind: 'update', user, details } : { kind: 'invite', details });
  }
  showHistory(item: ManagedUser | Invitation, invitation: boolean): void {
    this.selectedHistory.set({
      id: item.id,
      invitation,
      name: `${item.firstName} ${item.lastName}`,
    });
  }
  private matches(value: ManagedUser | Invitation): boolean {
    return `${value.firstName} ${value.lastName} ${value.email ?? ''} ${value.roles.join(' ')}`
      .toLowerCase()
      .includes(this.search().toLowerCase());
  }
  private async perform(action: Action): Promise<Invitation | void> {
    switch (action.kind) {
      case 'invite':
        return this.#api.invite(action.details);
      case 'update':
        return this.#api.update(action.user, action.details);
      case 'resend':
        return this.#api.resend(action.id);
      case 'revoke':
        return this.#api.revoke(action.id);
      case 'reset':
        return this.#api.resetPassword(action.id);
    }
  }
}
