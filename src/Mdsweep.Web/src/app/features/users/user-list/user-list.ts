import { Component, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronRight } from '@ng-icons/lucide';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmEmptyImports } from '@spartan-ng/helm/empty';
import { HlmTableImports } from '@spartan-ng/helm/table';

import { type UserListItem } from '../users.api';

@Component({
  selector: 'app-user-list',
  imports: [NgIcon, HlmBadge, ...HlmEmptyImports, ...HlmTableImports],
  providers: [provideIcons({ lucideChevronRight })],
  templateUrl: './user-list.html',
})
export default class UserList {
  readonly users = input.required<UserListItem[]>();
  readonly selectedUserKey = input<string | null>(null);
  readonly userSelected = output<UserListItem>();

  key(user: UserListItem): string {
    return `${user.type}:${user.id}`;
  }

  initials(user: UserListItem): string {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
  }

  statusLabel(user: UserListItem): string {
    return user.status === 'Inactive' ? 'Disabled' : user.status;
  }

  select(user: UserListItem): void {
    this.userSelected.emit(user);
  }
}
