import {
  afterNextRender,
  Component,
  computed,
  DOCUMENT,
  ElementRef,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { CdkTrapFocus } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { TranslatePipe } from '@ngx-translate/core';
import { injectMutation, injectQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucidePlus, lucideSearch } from '@ng-icons/lucide';
import { UiMessagePipe, type UiFeedback } from '@app/core/i18n/ui-message';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { VehiclesApi, type Vehicle, type VehicleDetails } from './vehicles.api';
import { vehicleQueryKeys, vehiclesQueryOptions } from './vehicles.queries';
import { VehicleForm } from './vehicle-form';
import { VehicleList } from './vehicle-list';
import { VehicleDetail } from './vehicle-detail';

type Filter = 'All' | 'Active' | 'Inactive';
type Action =
  | { kind: 'create'; details: VehicleDetails }
  | { kind: 'update'; id: string; details: VehicleDetails }
  | { kind: 'active'; id: string; isActive: boolean };

@Component({
  selector: 'app-vehicles-page',
  imports: [
    CdkTrapFocus,
    TranslatePipe,
    UiMessagePipe,
    HlmButton,
    HlmInput,
    HlmSpinner,
    NgIcon,
    VehicleForm,
    VehicleList,
    VehicleDetail,
    ...HlmAlertImports,
    ...HlmDialogImports,
  ],
  providers: [provideIcons({ lucidePlus, lucideSearch })],
  templateUrl: './vehicles-page.html',
  host: { class: 'block h-full min-h-0' },
})
export default class VehiclesPage {
  readonly #api = inject(VehiclesApi);
  readonly #queries = inject(QueryClient);
  readonly #document = inject(DOCUMENT);
  readonly #injector = inject(Injector);
  readonly #desktop = toSignal(inject(BreakpointObserver).observe('(min-width: 80rem)'));
  #detailTrigger: HTMLElement | null = null;
  readonly searchInput = viewChild.required<ElementRef<HTMLInputElement>>('searchInput');
  readonly mobileDetails = computed(() => !this.#desktop()?.matches && !!this.selectedVehicle());
  readonly listing = injectQuery(() => vehiclesQueryOptions(this.#api));
  readonly filters: Filter[] = ['All', 'Active', 'Inactive'];
  readonly search = signal('');
  readonly filter = signal<Filter>('All');
  readonly selectedId = signal<string | null>(null);
  readonly formOpen = signal(false);
  // Snapshot only the form's initial value; query refreshes must not erase an unsaved draft.
  readonly editing = signal<Vehicle | null>(null);
  readonly message = signal<UiFeedback | null>(null);
  readonly error = signal<UiFeedback | null>(null);
  readonly counts = computed(() => {
    const vehicles = this.listing.data() ?? [];
    const active = vehicles.filter((v) => v.isActive).length;
    return { All: vehicles.length, Active: active, Inactive: vehicles.length - active };
  });
  readonly vehicles = computed(() => {
    const search = this.search().trim().toLowerCase();
    return (this.listing.data() ?? []).filter(
      (v) =>
        (!search ||
          v.displayLabel.toLowerCase().includes(search) ||
          v.vin.toLowerCase().includes(search)) &&
        (this.filter() === 'All' || v.isActive === (this.filter() === 'Active')),
    );
  });
  readonly selectedVehicle = computed(
    () => (this.listing.data() ?? []).find((v) => v.id === this.selectedId()) ?? null,
  );
  readonly mutation = injectMutation(() => ({
    mutationFn: (action: Action) => this.perform(action),
    onSuccess: async (_: Vehicle | void, action: Action) => {
      this.formOpen.set(false);
      this.message.set({
        key:
          action.kind === 'create'
            ? 'vehicles.created'
            : action.kind === 'update'
              ? 'vehicles.updated'
              : action.isActive
                ? 'vehicles.reactivated'
                : 'vehicles.deactivated',
      });
      await this.#queries.invalidateQueries({ queryKey: vehicleQueryKeys.all });
    },
    onError: (error: unknown) => this.error.set(httpErrorMessage(error, 'errors.saveVehicle')),
  }));
  loadError(): UiFeedback {
    return httpErrorMessage(this.listing.error(), 'errors.loadVehicles');
  }
  startCreate(): void {
    this.editing.set(null);
    this.openForm();
  }
  startEdit(): void {
    this.editing.set(this.selectedVehicle());
    this.openForm();
  }
  private openForm(): void {
    this.error.set(null);
    this.message.set(null);
    this.formOpen.set(true);
  }
  select(vehicle: Vehicle): void {
    this.#detailTrigger = this.#document.activeElement as HTMLElement | null;
    this.selectedId.set(vehicle.id);
    this.error.set(null);
    this.message.set(null);
  }
  closeDetails(): void {
    if (this.mutation.isPending() || this.formOpen()) return;
    this.selectedId.set(null);
    afterNextRender(
      () => {
        // A status change can remove the original row from the current filter.
        const target = this.#detailTrigger?.isConnected
          ? this.#detailTrigger
          : this.searchInput().nativeElement;
        target.focus();
      },
      { injector: this.#injector },
    );
  }
  save(details: VehicleDetails): void {
    const editing = this.editing();
    this.run(editing ? { kind: 'update', id: editing.id, details } : { kind: 'create', details });
  }
  setActive(isActive: boolean): void {
    const vehicle = this.selectedVehicle();
    if (vehicle) this.run({ kind: 'active', id: vehicle.id, isActive });
  }
  private run(action: Action): void {
    if (this.mutation.isPending()) return;
    this.error.set(null);
    this.message.set(null);
    this.mutation.mutate(action);
  }
  private perform(action: Action): Promise<Vehicle | void> {
    switch (action.kind) {
      case 'create':
        return this.#api.create(action.details);
      case 'update':
        return this.#api.update(action.id, action.details);
      case 'active':
        return this.#api.setActive(action.id, action.isActive);
    }
  }
}
