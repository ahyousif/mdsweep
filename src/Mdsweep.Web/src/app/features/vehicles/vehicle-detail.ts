import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCar, lucideX, lucidePen, lucideBan, lucideRotateCcw } from '@ng-icons/lucide';
import { type Vehicle } from './vehicles.api';

@Component({
  selector: 'app-vehicle-detail',
  imports: [TranslatePipe, HlmButton, HlmBadge, NgIcon],
  providers: [provideIcons({ lucideCar, lucideX, lucidePen, lucideBan, lucideRotateCcw })],
  templateUrl: './vehicle-detail.html',
})
export class VehicleDetail {
  readonly vehicle = input.required<Vehicle>();
  // The page supplies its mutation state to prevent conflicting actions during a save.
  readonly mutationPending = input(false);
  readonly closed = output();
  readonly editClicked = output();
  readonly activeChanged = output<boolean>();
}
