import { TranslatePipe } from '@ngx-translate/core';
import { Component, input, output } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideSearch } from '@ng-icons/lucide';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';

@Component({
  selector: 'app-passenger-toolbar',
  imports: [TranslatePipe, NgIcon, HlmButton, HlmInput],
  providers: [provideIcons({ lucidePlus, lucideSearch })],
  templateUrl: './passenger-toolbar.html',
})
export default class PassengerToolbar {
  readonly search = input('');
  readonly busy = input(false);
  readonly searchChange = output<string>();
  readonly addClicked = output<void>();

  onSearchInput(event: Event): void {
    this.searchChange.emit((event.target as HTMLInputElement).value);
  }
}
