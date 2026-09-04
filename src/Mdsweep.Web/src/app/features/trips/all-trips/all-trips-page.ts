import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';
import { HlmTableImports } from '@spartan-ng/helm/table';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { ColumnDef, FlexRender, injectTable, tableFeatures } from '@tanstack/angular-table';
import { QueryClient } from '@tanstack/query-core';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { uiText } from '@app/ui-text';
import { AllTripsApi, AllTripsTrip, TripsQuery } from './all-trips.api';
import { allTripsQueryOptions, tripQueryKeys } from './all-trips.queries';

const features = tableFeatures({});
const serviceDateFormatter = new Intl.DateTimeFormat(undefined, {
  month: 'short',
  day: 'numeric',
  year: 'numeric',
});
const serviceDateEmptyStateFormatter = new Intl.DateTimeFormat(undefined, {
  month: 'long',
  day: 'numeric',
});

function toServiceDate(date: Date): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');

  return `${year}-${month}-${day}`;
}

function toLocalDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);

  return new Date(year, month - 1, day);
}

const allTripsColumns: ColumnDef<typeof features, AllTripsTrip>[] = [
  { id: 'passenger', header: 'Passenger', accessorFn: (row) => `${row.passengerFirstName} ${row.passengerLastName}` },
  {
    id: 'scheduledPickupTime',
    header: 'Scheduled pickup',
    accessorFn: (row) => row.scheduledPickupTime ?? '',
  },
  {
    id: 'appointment',
    header: 'Appointment',
    accessorFn: (row) => (row.isWillCall ? 'Will call' : (row.appointmentTime ?? '')),
  },
  {
    id: 'route',
    header: 'Route',
    accessorFn: (row) => `${row.pickupAddress} → ${row.dropoffAddress}`,
  },
  { id: 'attention', header: 'Attention', accessorFn: (row) => attentionText(row) },
];

function attentionText(trip: AllTripsTrip): string {
  if (trip.brokerStatus && trip.brokerStatus !== 'VALID') return `Broker: ${trip.brokerStatus}`;
  if (!trip.isWillCall && !trip.scheduledPickupTime) return 'Set pickup time';
  return '';
}

@Component({
  selector: 'app-all-trips-page',
  imports: [
    FlexRender,
    NgIcon,
    HlmButton,
    ...HlmBadgeImports,
    ...HlmDatePickerImports,
    HlmInput,
    HlmSpinner,
    ...HlmAlertImports,
    ...HlmCardImports,
    ...HlmSheetImports,
    ...HlmTableImports,
    RouterLink,
  ],
  providers: [provideIcons({ lucideChevronLeft, lucideChevronRight })],
  templateUrl: './all-trips-page.html',
})
export default class AllTripsPage {
  readonly #api = inject(AllTripsApi);
  readonly #queryClient = inject(QueryClient);

  readonly text = uiText;
  readonly serviceDate = signal(toServiceDate(new Date()));
  readonly endDate = signal(toServiceDate(new Date()));
  readonly search = signal('');
  readonly needsAttention = signal(false);
  readonly page = signal(1);
  readonly selectedTrip = signal<AllTripsTrip | null>(null);
  readonly scheduleError = signal('');
  readonly selectedServiceDate = computed(() => toLocalDate(this.serviceDate()));
  readonly isToday = computed(() => this.serviceDate() === toServiceDate(new Date()) && this.endDate() === this.serviceDate());
  readonly isMultiDay = computed(() => this.serviceDate() !== this.endDate());
  readonly query = computed<TripsQuery>(() => ({
    startDate: this.serviceDate(), endDate: this.endDate(), search: this.search() || undefined,
    needsAttention: this.needsAttention() || undefined, page: this.page(),
  }));
  readonly emptyStateText = computed(() =>
    this.isToday()
      ? 'No trips scheduled for today.'
      : `No trips scheduled for ${serviceDateEmptyStateFormatter.format(this.selectedServiceDate())}.`,
  );
  readonly formatServiceDate = (date: Date): string =>
    toServiceDate(date) === toServiceDate(new Date())
      ? `Today · ${serviceDateFormatter.format(date)}`
      : serviceDateFormatter.format(date);

  readonly tripsQuery = injectQuery(() =>
    allTripsQueryOptions(this.#api, this.query()),
  );

  readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ id, value }: { id: string; value: string }) =>
      this.#api.setScheduledPickupTime(id, value),
    onSuccess: () =>
      this.#queryClient.invalidateQueries({
        queryKey: tripQueryKeys.all,
      }),
    onError: (error) =>
      this.scheduleError.set(httpErrorMessage(error, this.text.scheduleSaveError)),
  }));

  readonly busy = computed(() => this.scheduleMutation.isPending());

  readonly dispatchTable = injectTable(() => ({
    features,
    columns: allTripsColumns,
    data: this.tripsQuery.data()?.items ?? [],
  }));

  setServiceDate(date: Date | null): void {
    if (date) {
      this.serviceDate.set(toServiceDate(date));
      this.endDate.set(toServiceDate(date));
      this.page.set(1);
    }
  }

  moveServiceDate(days: number): void {
    const date = this.selectedServiceDate();
    date.setDate(date.getDate() + days);
    this.serviceDate.set(toServiceDate(date));
    this.endDate.set(this.serviceDate());
    this.page.set(1);
  }

  goToToday(): void {
    this.serviceDate.set(toServiceDate(new Date()));
    this.endDate.set(this.serviceDate());
    this.page.set(1);
  }

  tripsQueryError(): string {
    return httpErrorMessage(this.tripsQuery.error(), 'The trips could not be loaded. Try again.');
  }

  setScheduledPickupTime(trip: AllTripsTrip, value: string): void {
    if (!value) return;
    this.scheduleError.set('');
    this.scheduleMutation.mutate({ id: trip.id, value });
  }

  setThisWeek(): void {
    const start = new Date();
    start.setDate(start.getDate() - start.getDay());
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    this.serviceDate.set(toServiceDate(start));
    this.endDate.set(toServiceDate(end));
    this.page.set(1);
  }

  setTomorrow(): void { this.moveServiceDate(1); }

  setSearch(value: string): void { this.search.set(value); this.page.set(1); }

  toggleAttention(): void { this.needsAttention.update(value => !value); this.page.set(1); }

  setPage(page: number): void { this.page.set(page); }

  attentionText = attentionText;

}
