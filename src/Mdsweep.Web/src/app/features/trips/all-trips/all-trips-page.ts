import { Component, computed, inject, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmBadge } from '@spartan-ng/helm/badge';
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
  {
    id: 'appointment',
    header: 'Appointment',
    accessorFn: (row) => (row.isWillCall ? 'Will call' : row.appointmentTime),
  },
  { accessorKey: 'memberName', header: 'Passenger' },
  {
    id: 'route',
    header: 'Route',
    accessorFn: (row) => `${row.pickupCity} → ${row.deliveryCity}`,
  },
  {
    id: 'service',
    header: 'Service',
    accessorFn: (row) => row.vehicleType || row.passengerType,
  },
  {
    id: 'status',
    header: 'Status',
    accessorFn: (row) => (row.isActive ? 'Ready' : row.brokerStatus),
  },
];

@Component({
  selector: 'app-all-trips-page',
  imports: [
    FlexRender,
    HlmBadge,
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
  protected readonly serviceDate = signal(new Date().toISOString().slice(0, 10));
  protected readonly error = signal('');

  protected readonly conflictsQuery = injectQuery(() => ({
    queryKey: tripQueryKeys.conflicts(),
    queryFn: () => this.api.getConflicts(),
  }));

  protected readonly serviceDayQuery = injectQuery(() =>
    allTripsQueryOptions(this.api, this.serviceDate()),
  );

  protected readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ tripNumber, value }: { tripNumber: string; value: string }) =>
      this.api.setScheduledPickupTime(tripNumber, value),
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
    data: this.serviceDayQuery.data() ?? [],
  }));

  protected setScheduledPickupTime(trip: AllTripsTrip, value: string): void {
    if (!value) return;
    this.error.set('');
    this.scheduleMutation.mutate({ tripNumber: trip.tripNumber, value });
  }

}
