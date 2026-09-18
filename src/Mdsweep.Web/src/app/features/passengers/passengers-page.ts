import { UiMessagePipe, type UiFeedback } from '@app/core/i18n/ui-message';
import { TranslatePipe } from '@ngx-translate/core';
import { Component, computed, inject, signal } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';

import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmEmptyImports } from '@spartan-ng/helm/empty';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectMutation, injectQuery, QueryClient } from '@tanstack/angular-query-experimental';

import { httpErrorMessage } from '@app/core/api/http-error-message';

import PassengerDetail from './passenger-detail';
import PassengerForm from './passenger-form';
import PassengerList from './passenger-list';
import PassengerToolbar from './passenger-toolbar';
import {
  PassengersApi,
  type CreatePassengerDetails,
  type Passenger,
  type PassengerDetails,
  type PassengersQuery,
} from './passengers.api';
import {
  passengerDetailQueryOptions,
  passengerQueryKeys,
  passengersQueryOptions,
} from './passengers.queries';

const pageSize = 25;

@Component({
  selector: 'app-passengers-page',
  imports: [
    UiMessagePipe,
    TranslatePipe,
    NgIcon,
    HlmButton,
    HlmSpinner,
    PassengerDetail,
    PassengerForm,
    PassengerList,
    PassengerToolbar,
    ...HlmAlertImports,
    ...HlmDialogImports,
    ...HlmEmptyImports,
  ],
  providers: [provideIcons({ lucideChevronLeft, lucideChevronRight })],
  templateUrl: './passengers-page.html',
  host: { class: 'block h-full min-h-0' },
})
export default class PassengersPage {
  readonly #api = inject(PassengersApi);
  readonly #queries = inject(QueryClient);
  readonly search = signal('');
  readonly page = signal(1);
  readonly selectedPassengerId = signal<string | null>(null);
  readonly creating = signal(false);
  readonly editing = signal(false);
  readonly confirmingDisable = signal(false);
  readonly message = signal<UiFeedback | null>(null);
  readonly error = signal<UiFeedback | null>(null);

  readonly query = computed<PassengersQuery>(() => ({
    search: this.search().trim() || undefined,
    page: this.page(),
    pageSize,
  }));
  readonly listing = injectQuery(() => passengersQueryOptions(this.#api, this.query()));
  readonly detail = injectQuery(() =>
    passengerDetailQueryOptions(this.#api, this.selectedPassengerId()),
  );
  readonly passengers = computed(() => this.listing.data()?.items ?? []);
  readonly totalPages = computed(() => this.listing.data()?.totalPages ?? 0);
  readonly pageRange = computed(() => {
    const listing = this.listing.data();
    if (!listing?.totalCount) return null;
    const start = (listing.page - 1) * listing.pageSize + 1;
    return { start, end: Math.min(listing.totalCount, start + listing.items.length - 1), total: listing.totalCount };
  });
  readonly mutation = injectMutation(() => ({
    mutationFn: (details: CreatePassengerDetails) => this.#api.create(details),
    onSuccess: async (passenger: Passenger) => {
      this.creating.set(false);
      this.message.set({ key: 'passengers.created', params: { name: this.fullName(passenger) } });
      await this.#queries.invalidateQueries({ queryKey: passengerQueryKeys.all });
      this.selectedPassengerId.set(passenger.id);
    },
    onError: (error: unknown) => this.error.set(httpErrorMessage(error, 'errors.savePassenger')),
  }));
  readonly updateMutation = injectMutation(() => ({
    mutationFn: ({ id, details }: { id: string; details: PassengerDetails }) =>
      this.#api.update(id, details),
    onSuccess: async () => {
      this.editing.set(false);
      await this.#queries.invalidateQueries({ queryKey: passengerQueryKeys.all });
    },
    onError: (error: unknown) => this.error.set(httpErrorMessage(error, 'errors.savePassenger')),
  }));
  readonly disableMutation = injectMutation(() => ({
    mutationFn: (id: string) => this.#api.disable(id),
    onSuccess: async () => {
      this.confirmingDisable.set(false);
      await this.#queries.invalidateQueries({ queryKey: passengerQueryKeys.all });
    },
    onError: (error: unknown) => this.error.set(httpErrorMessage(error, 'errors.savePassenger')),
  }));
  readonly enableMutation = injectMutation(() => ({
    mutationFn: (id: string) => this.#api.enable(id),
    onSuccess: async () => {
      await this.#queries.invalidateQueries({ queryKey: passengerQueryKeys.all });
    },
    onError: (error: unknown) => this.error.set(httpErrorMessage(error, 'errors.savePassenger')),
  }));

  loadError(): UiFeedback { return httpErrorMessage(this.listing.error(), 'errors.loadPassengers'); }
  detailError(): UiFeedback { return httpErrorMessage(this.detail.error(), 'errors.loadPassenger'); }
  fullName(passenger: Passenger): string { return `${passenger.firstName} ${passenger.lastName}`; }

  setSearch(value: string): void {
    this.search.set(value);
    this.page.set(1);
    this.closeDetail();
  }

  selectPassenger(passenger: Passenger): void {
    this.selectedPassengerId.set(passenger.id);
    this.editing.set(false);
    this.error.set(null);
  }

  closeDetail(): void {
    this.selectedPassengerId.set(null);
    this.editing.set(false);
  }
  editPassenger(): void {
    this.error.set(null);
    this.editing.set(true);
  }
  updatePassenger(details: PassengerDetails): void {
    const passenger = this.detail.data();
    if (!passenger || this.updateMutation.isPending()) return;
    this.error.set(null);
    this.updateMutation.mutate({ id: passenger.id, details });
  }
  disablePassenger(): void {
    const passenger = this.detail.data();
    if (!passenger || this.disableMutation.isPending()) return;
    this.error.set(null);
    this.disableMutation.mutate(passenger.id);
  }
  enablePassenger(): void {
    const passenger = this.detail.data();
    if (!passenger || this.enableMutation.isPending()) return;
    this.error.set(null);
    this.enableMutation.mutate(passenger.id);
  }
  startCreate(): void { this.error.set(null); this.message.set(null); this.creating.set(true); }
  create(details: CreatePassengerDetails): void { if (!this.mutation.isPending()) this.mutation.mutate(details); }
  previousPage(): void { if (this.page() > 1) { this.page.update((page) => page - 1); this.closeDetail(); } }
  nextPage(): void { if (this.page() < this.totalPages()) { this.page.update((page) => page + 1); this.closeDetail(); } }
}
