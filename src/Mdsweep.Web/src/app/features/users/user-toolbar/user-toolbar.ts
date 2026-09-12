import { TranslatePipe } from '@ngx-translate/core';
import { Component, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideSearch } from '@ng-icons/lucide';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';

export type UserStatusFilter = 'All' | 'Active' | 'Invited' | 'Disabled';
export type UserStatusCounts = Record<UserStatusFilter, number>;

export const userStatusFilters: UserStatusFilter[] = ['All', 'Active', 'Invited', 'Disabled'];

@Component({
  selector: 'app-user-toolbar',
  imports: [TranslatePipe, NgIcon, HlmButton, HlmInput],
  providers: [provideIcons({ lucidePlus, lucideSearch })],
  templateUrl: './user-toolbar.html',
})
export default class UserToolbar {
  readonly search = input('');
  readonly activeFilter = input.required<UserStatusFilter>();
  readonly counts = input.required<UserStatusCounts>();
  readonly busy = input(false);

  readonly searchChange = output<string>();
  readonly filterChange = output<UserStatusFilter>();
  readonly inviteClicked = output<void>();

  readonly filters = userStatusFilters;

  onSearchInput(event: Event): void {
    this.searchChange.emit((event.target as HTMLInputElement).value);
  }
}
