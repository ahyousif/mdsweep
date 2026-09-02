import { Component, computed, inject, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmTableImports } from '@spartan-ng/helm/table';
import {
  injectMutation,
  injectQuery,
  injectQueryClient,
} from '@tanstack/angular-query-experimental';
import { ColumnDef, FlexRender, injectTable, tableFeatures } from '@tanstack/angular-table';
import { httpErrorMessage } from '../../../core/api/http-error-message';
import { uiText } from '../../../ui-text';
import { AllTripsApi, AllTripsTrip } from './all-trips.api';
import { tripQueryKeys, allTripsQueryOptions } from './all-trips.queries';

const features = tableFeatures({});

const allTripsColumns: ColumnDef<typeof features, AllTripsTrip>[] = [
  {
    id: 'scheduledPickupTime',
    header: 'Scheduled pickup',
    accessorFn: (row) => row.scheduledPickupTime ?? '',
  },
  { id: 'appointment', header: 'Appointment', accessorFn: (row) => row.isWillCall ? 'Will call' : row.appointmentTime ?? '' },
  { accessorKey: 'brokerTripNumber', header: 'Trip' },
  { id: 'pickup', header: 'Pickup', accessorFn: (row) => `${row.pickupAddress}, ${row.pickupCity}` },
  { id: 'dropoff', header: 'Drop-off', accessorFn: (row) => `${row.dropoffAddress}, ${row.dropoffCity}` },
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
  ],
  templateUrl: './all-trips-page.html',
})
export class AllTripsPage {
  private readonly api = inject(AllTripsApi);
  private readonly queryClient = injectQueryClient();

  protected readonly text = uiText;
  protected readonly serviceDate = signal(localCalendarDate());
  protected readonly error = signal('');

  protected readonly serviceDayQuery = injectQuery(() =>
    allTripsQueryOptions(this.api, this.serviceDate()),
  );

  protected readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ id, value }: { id: string; value: string }) => this.api.setScheduledPickupTime(id, value),
    onSuccess: () =>
      this.queryClient.invalidateQueries({
        queryKey: tripQueryKeys.serviceDate(this.serviceDate()),
      }),
    onError: (error) => this.error.set(httpErrorMessage(error, this.text.scheduleSaveError)),
  }));

  protected readonly busy = computed(
    () =>
      this.scheduleMutation.isPending(),
  );

  protected readonly dispatchTable = injectTable(() => ({
    features,
    columns: allTripsColumns,
    data: this.serviceDayQuery.data()?.items ?? [],
  }));

  protected setScheduledPickupTime(trip: AllTripsTrip, value: string): void {
    if (!value) return;
    this.error.set('');
    this.scheduleMutation.mutate({ id: trip.id, value });
  }

}

function localCalendarDate(date = new Date()): string {
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}
