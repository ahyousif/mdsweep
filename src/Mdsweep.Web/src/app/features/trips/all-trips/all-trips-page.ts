import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmTableImports } from '@spartan-ng/helm/table';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { ColumnDef, FlexRender, injectTable, tableFeatures } from '@tanstack/angular-table';
import { QueryClient } from '@tanstack/query-core';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { uiText } from '@app/ui-text';
import { AllTripsApi, AllTripsTrip } from './all-trips.api';
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
  { accessorKey: 'brokerTripNumber', header: 'Trip' },
  {
    id: 'pickup',
    header: 'Pickup',
    accessorFn: (row) => `${row.pickupAddress}, ${row.pickupCity}`,
  },
  {
    id: 'dropoff',
    header: 'Drop-off',
    accessorFn: (row) => `${row.dropoffAddress}, ${row.dropoffCity}`,
  },
  { accessorKey: 'brokerStatus', header: 'Broker status' },
];

@Component({
  selector: 'app-all-trips-page',
  imports: [
    FlexRender,
    NgIcon,
    HlmButton,
    ...HlmDatePickerImports,
    HlmInput,
    HlmSpinner,
    ...HlmAlertImports,
    ...HlmCardImports,
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
  readonly scheduleError = signal('');
  readonly selectedServiceDate = computed(() => toLocalDate(this.serviceDate()));
  readonly isToday = computed(() => this.serviceDate() === toServiceDate(new Date()));
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
    allTripsQueryOptions(this.#api, this.serviceDate()),
  );

  readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ id, value }: { id: string; value: string }) =>
      this.#api.setScheduledPickupTime(id, value),
    onSuccess: () =>
      this.#queryClient.invalidateQueries({
        queryKey: tripQueryKeys.serviceDate(this.serviceDate()),
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
    }
  }

  moveServiceDate(days: number): void {
    const date = this.selectedServiceDate();
    date.setDate(date.getDate() + days);
    this.serviceDate.set(toServiceDate(date));
  }

  goToToday(): void {
    this.serviceDate.set(toServiceDate(new Date()));
  }

  tripsQueryError(): string {
    return httpErrorMessage(this.tripsQuery.error(), 'The trips could not be loaded. Try again.');
  }

  setScheduledPickupTime(trip: AllTripsTrip, value: string): void {
    if (!value) return;
    this.scheduleError.set('');
    this.scheduleMutation.mutate({ id: trip.id, value });
  }

}
