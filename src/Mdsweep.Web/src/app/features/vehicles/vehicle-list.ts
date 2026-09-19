import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { HlmTableImports } from '@spartan-ng/helm/table';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideCar, lucideChevronRight } from '@ng-icons/lucide';
import { type Vehicle } from './vehicles.api';

@Component({
  selector: 'app-vehicle-list',
  imports: [TranslatePipe, HlmBadge, NgIcon, ...HlmTableImports],
  providers: [provideIcons({ lucideCar, lucideChevronRight })],
  templateUrl: './vehicle-list.html',
})
export class VehicleList {
  readonly vehicles = input.required<Vehicle[]>();
  readonly selectedId = input<string | null>(null);
  readonly mutationPending = input(false);
  readonly filtered = input(false);
  readonly selected = output<Vehicle>();
}
