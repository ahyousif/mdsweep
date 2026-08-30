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
import {
  ColumnDef,
  FlexRender,
  injectTable,
  tableFeatures,
} from '@tanstack/angular-table';
import { httpErrorMessage } from '../../core/api/http-error-message';
import { uiText } from '../../ui-text';
import { DispatchApi, DispatchTrip } from './data-access/dispatch.api';
import {
  ManifestImportApi,
  ManifestPreview,
  PreviewRow,
} from '../manifest-import/data-access/manifest-import.api';

const features = tableFeatures({});

const previewColumns: ColumnDef<typeof features, PreviewRow>[] = [
  { accessorKey: 'tripNumber', header: 'Trip' },
  { accessorKey: 'disposition', header: 'Disposition' },
  { accessorKey: 'brokerChange', header: 'MTM change' },
  {
    id: 'messages',
    header: 'Review notes',
    accessorFn: (row) => row.messages.join(' '),
  },
];

const dispatchColumns: ColumnDef<typeof features, DispatchTrip>[] = [
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
  selector: 'app-dispatch-page',
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
  templateUrl: './dispatch-page.html',
})
export class DispatchPage {
  private readonly manifests = inject(ManifestImportApi);
  private readonly dispatch = inject(DispatchApi);
  private readonly queryClient = injectQueryClient();

  protected readonly text = uiText;
  protected readonly preview = signal<ManifestPreview | null>(null);
  protected readonly serviceDate = signal('');
  protected readonly error = signal('');

  protected readonly conflictsQuery = injectQuery(() => ({
    queryKey: ['dispatch', 'driver-conflicts'],
    queryFn: () => this.dispatch.getConflicts(),
  }));

  protected readonly serviceDayQuery = injectQuery(() => ({
    queryKey: ['dispatch', 'service-day', this.serviceDate()],
    queryFn: () => this.dispatch.getServiceDay(this.serviceDate()),
    enabled: this.serviceDate().length > 0,
  }));

  protected readonly previewMutation = injectMutation(() => ({
    mutationFn: (file: File) => this.manifests.preview(file),
    onSuccess: (preview) => {
      this.preview.set(preview);
      this.serviceDate.set(preview.serviceDates[0] ?? '');
    },
    onError: (error) =>
      this.error.set(httpErrorMessage(error, 'Unable to check this Manifest.')),
  }));

  protected readonly applyMutation = injectMutation(() => ({
    mutationFn: (previewId: string) => this.manifests.apply(previewId),
    onSuccess: () =>
      this.queryClient.invalidateQueries({
        queryKey: ['dispatch', 'service-day', this.serviceDate()],
      }),
    onError: (error) =>
      this.error.set(httpErrorMessage(error, 'Unable to import this Manifest.')),
  }));

  protected readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ tripNumber, value }: { tripNumber: string; value: string }) =>
      this.dispatch.setScheduledPickupTime(tripNumber, value),
    onSuccess: () =>
      this.queryClient.invalidateQueries({
        queryKey: ['dispatch', 'service-day', this.serviceDate()],
      }),
    onError: (error) =>
      this.error.set(httpErrorMessage(error, this.text.scheduleSaveError)),
  }));

  protected readonly busy = computed(
    () =>
      this.previewMutation.isPending() ||
      this.applyMutation.isPending() ||
      this.scheduleMutation.isPending(),
  );

  protected readonly previewTable = injectTable(() => ({
    features,
    columns: previewColumns,
    data: this.preview()?.rows ?? [],
  }));

  protected readonly dispatchTable = injectTable(() => ({
    features,
    columns: dispatchColumns,
    data: this.serviceDayQuery.data() ?? [],
  }));

  protected chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.error.set('');
    this.previewMutation.mutate(file);
  }

  protected apply(): void {
    const preview = this.preview();
    if (!preview) return;
    this.error.set('');
    this.applyMutation.mutate(preview.previewId);
  }

  protected setScheduledPickupTime(trip: DispatchTrip, value: string): void {
    if (!value) return;
    this.error.set('');
    this.scheduleMutation.mutate({ tripNumber: trip.tripNumber, value });
  }

  protected countBrokerChanges(change: PreviewRow['brokerChange']): number {
    return this.preview()?.rows.filter((row) => row.brokerChange === change).length ?? 0;
  }

  protected countProviderOverrides(): number {
    return this.preview()?.rows.filter((row) => row.hasProviderOverrides).length ?? 0;
  }

  protected countInactive(): number {
    return (
      this.preview()?.rows.filter(
        (row) => !row.isActive && row.disposition !== 'Blocked',
      ).length ?? 0
    );
  }
}
