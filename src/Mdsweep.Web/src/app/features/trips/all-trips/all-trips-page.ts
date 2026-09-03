import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
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
    HlmButton,
    HlmInput,
    HlmSpinner,
    ...HlmAlertImports,
    ...HlmCardImports,
    ...HlmTableImports,
    RouterLink,
  ],
  templateUrl: './all-trips-page.html',
})
export default class AllTripsPage {
  readonly #api = inject(AllTripsApi);
  readonly #queryClient = inject(QueryClient);

  readonly text = uiText;
  readonly serviceDateFilter = signal('');
  readonly scheduleError = signal('');

  readonly tripsQuery = injectQuery(() =>
    allTripsQueryOptions(this.#api, this.serviceDateFilter()),
  );

  readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ id, value }: { id: string; value: string }) =>
      this.#api.setScheduledPickupTime(id, value),
    onSuccess: () =>
      this.#queryClient.invalidateQueries({
        queryKey: tripQueryKeys.serviceDate(this.serviceDateFilter()),
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

  setServiceDateFilter(value: string): void {
    this.serviceDateFilter.set(value);
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
