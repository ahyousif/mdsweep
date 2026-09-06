import { NgTemplateOutlet } from '@angular/common';
import { BreakpointObserver } from '@angular/cdk/layout';
import { toSignal } from '@angular/core/rxjs-interop';
import { HlmSkeleton } from '@spartan-ng/helm/skeleton';
import {
  afterNextRender,
  Component,
  computed,
  DestroyRef,
  effect,
  ElementRef,
  inject,
  Injector,
  signal,
  viewChild,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChevronLeft, lucideChevronRight } from '@ng-icons/lucide';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmBadgeImports } from '@spartan-ng/helm/badge';
import { HlmDatePickerImports } from '@spartan-ng/helm/date-picker';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmSheetImports } from '@spartan-ng/helm/sheet';
import { HlmPopoverImports } from '@spartan-ng/helm/popover';
import { HlmTableImports } from '@spartan-ng/helm/table';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import {
  ColumnDef,
  FlexRender,
  injectTable,
  tableFeatures,
  rowSortingFeature,
} from '@tanstack/angular-table';
import { QueryClient } from '@tanstack/query-core';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { uiText } from '@app/ui-text';
import { AllTripsApi, AllTripsTrip, TripsQuery, TripsResponse } from './all-trips.api';
import { allTripsQueryOptions, tripQueryKeys } from './all-trips.queries';

type TripDateScope = 'day' | 'week';

const features = tableFeatures({ rowSortingFeature });
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

const baseColumns: ColumnDef<typeof features, AllTripsTrip>[] = [
  {
    id: 'passenger',
    header: 'Passenger',
    accessorFn: (row) => `${row.passengerFirstName} ${row.passengerLastName}`,
  },
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
    enableSorting: false,
    header: 'Route',
    accessorFn: (row) => `${row.pickupAddress} → ${row.dropoffAddress}`,
  },
  {
    id: 'attention',
    enableSorting: false,
    header: 'Attention',
    accessorFn: (row) => attentionText(row),
  },
];

export function attentionReasons(
  trip: AllTripsTrip,
): { text: string; variant: 'destructive' | 'secondary' }[] {
  const reasons: { text: string; variant: 'destructive' | 'secondary' }[] = [];
  if (trip.brokerStatus !== null && trip.brokerStatus !== 'VALID')
    reasons.push({ text: `Broker: ${trip.brokerStatus || 'Unknown'}`, variant: 'destructive' });
  if (trip.mobilityRequirement === 'Unknown')
    reasons.push({ text: 'Mobility unknown', variant: 'secondary' });
  return reasons;
}

function attentionText(trip: AllTripsTrip): string {
  return attentionReasons(trip)
    .map((reason) => reason.text)
    .join(' · ');
}

const mobilityLabels: Record<AllTripsTrip['mobilityRequirement'], string> = {
  Ambulatory: '',
  Cane: 'Cane',
  ManualWheelchair: 'Wheelchair',
  ManualWheelchairCannotTransfer: 'Wheelchair · cannot transfer',
  ElectricWheelchair: 'Electric wheelchair',
  Unknown: 'Mobility unknown',
};
export function formatMobility(value: AllTripsTrip['mobilityRequirement']): string {
  return mobilityLabels[value];
}
const timeFormatter = new Intl.DateTimeFormat('en-US', {
  hour: 'numeric',
  minute: '2-digit',
  hour12: true,
});
export function formatTime(value: string | null): string {
  if (!value) return 'Not supplied';
  const [hours, minutes] = value.split(':').map(Number);
  return timeFormatter.format(new Date(2000, 0, 1, hours, minutes));
}
const sortColumns: Record<string, string> = {
  passenger: 'PassengerName',
  scheduledPickupTime: 'ScheduledPickupTime',
  appointment: 'AppointmentTime',
  serviceDate: 'ServiceDate',
};

@Component({
  selector: 'app-all-trips-page',
  imports: [
    FlexRender,
    NgTemplateOutlet,
    HlmSkeleton,
    NgIcon,
    HlmButton,
    ...HlmBadgeImports,
    ...HlmDatePickerImports,
    HlmInput,
    HlmSpinner,
    ...HlmAlertImports,
    ...HlmSheetImports,
    ...HlmPopoverImports,
    ...HlmTableImports,
    RouterLink,
  ],
  providers: [provideIcons({ lucideChevronLeft, lucideChevronRight })],
  templateUrl: './all-trips-page.html',
})
export default class AllTripsPage {
  readonly desktop = toSignal(inject(BreakpointObserver).observe('(min-width: 1024px)'));
  readonly toolbar = viewChild<ElementRef<HTMLElement>>('toolbar');
  readonly toolbarHeight = signal(0);
  readonly #injector = inject(Injector);
  readonly #element = inject<ElementRef<HTMLElement>>(ElementRef);
  readonly #destroyRef = inject(DestroyRef);
  #searchTimer: ReturnType<typeof setTimeout> | undefined;
  readonly debouncedSearch = signal('');
  readonly editingTripId = signal<string | null>(null);
  readonly draftTime = signal('');
  readonly #displayedKey = signal<ReturnType<typeof tripQueryKeys.workspace> | null>(null);
  readonly #api = inject(AllTripsApi);
  readonly #queryClient = inject(QueryClient);

  readonly text = uiText;
  readonly serviceDate = signal(toServiceDate(new Date()));
  readonly endDate = signal(toServiceDate(new Date()));
  readonly dateScope = signal<TripDateScope>('day');
  readonly search = signal('');
  readonly needsAttention = signal(false);
  readonly page = signal(1);
  readonly sortBy = signal('ScheduledPickupTime');
  readonly sortDirection = signal<'Ascending' | 'Descending'>('Ascending');
  readonly selectedTrip = signal<AllTripsTrip | null>(null);
  readonly scheduleError = signal('');
  readonly selectedServiceDate = computed(() => toLocalDate(this.serviceDate()));
  readonly isToday = computed(
    () => this.serviceDate() === toServiceDate(new Date()) && this.endDate() === this.serviceDate(),
  );
  readonly isMultiDay = computed(() => this.serviceDate() !== this.endDate());
  readonly query = computed<TripsQuery>(() => ({
    startDate: this.serviceDate(),
    endDate: this.endDate(),
    search: this.debouncedSearch() || undefined,
    needsAttention: this.needsAttention() || undefined,
    page: this.page(),
    sortBy: this.sortBy(),
    sortDirection: this.sortDirection(),
  }));
  readonly columns = computed(() =>
    this.isMultiDay()
      ? [
          { id: 'serviceDate', header: 'Trip date', accessorKey: 'serviceDate' } as ColumnDef<
            typeof features,
            AllTripsTrip
          >,
          ...baseColumns,
        ]
      : baseColumns,
  );
  readonly emptyStateText = computed(() =>
    this.search() || this.needsAttention()
      ? 'No trips match these filters.'
      : this.isMultiDay()
        ? `No trips for ${this.scopeLabel()}.`
        : this.isToday()
          ? 'No trips scheduled for today.'
          : `No trips scheduled for ${serviceDateEmptyStateFormatter.format(this.selectedServiceDate())}.`,
  );
  readonly formatServiceDate = (date: Date): string =>
    toServiceDate(date) === toServiceDate(new Date())
      ? `Today · ${serviceDateFormatter.format(date)}`
      : serviceDateFormatter.format(date);

  readonly tripsQuery = injectQuery(() => allTripsQueryOptions(this.#api, this.query()));

  readonly data = computed(
    () =>
      this.tripsQuery.data() ??
      (this.#displayedKey()
        ? this.#queryClient.getQueryData<TripsResponse>(this.#displayedKey()!)
        : undefined),
  );
  readonly scopeLabel = computed(() =>
    this.isMultiDay()
      ? `${serviceDateFormatter.format(this.selectedServiceDate())} – ${serviceDateFormatter.format(toLocalDate(this.endDate()))}`
      : this.formatServiceDate(this.selectedServiceDate()),
  );
  readonly isTomorrow = computed(() => {
    const date = new Date();
    date.setDate(date.getDate() + 1);
    return !this.isMultiDay() && this.serviceDate() === toServiceDate(date);
  });
  readonly isThisWeek = computed(() => {
    const date = new Date();
    date.setDate(date.getDate() - date.getDay());
    return this.dateScope() === 'week' && this.serviceDate() === toServiceDate(date);
  });
  readonly paginationText = computed(() => {
    const data = this.data();
    if (!data) return '';
    if (data.totalPages <= 1) return `${data.totalCount} trips`;
    return `${(data.page - 1) * data.pageSize + 1}–${Math.min(data.page * data.pageSize, data.totalCount)} of ${data.totalCount}`;
  });

  constructor() {
    effect(() => {
      if (this.tripsQuery.isSuccess() && !this.tripsQuery.isPlaceholderData()) {
        this.#displayedKey.set(tripQueryKeys.workspace(this.query()));
      }
    });
    afterNextRender(() => {
      const element = this.toolbar()!.nativeElement;
      const observer = new ResizeObserver(() =>
        this.toolbarHeight.set(element.getBoundingClientRect().height),
      );
      observer.observe(element);
      this.#destroyRef.onDestroy(() => observer.disconnect());
    });
    this.#destroyRef.onDestroy(() => clearTimeout(this.#searchTimer));
  }

  readonly scheduleMutation = injectMutation(() => ({
    mutationFn: ({ id, value }: { id: string; value: string }) =>
      this.#api.setScheduledPickupTime(id, value),
    onSuccess: async (_, variables) => {
      this.editingTripId.set(null);
      this.focusPickup(variables.id);
      this.selectedTrip.update((trip) =>
        trip?.id === variables.id ? { ...trip, scheduledPickupTime: variables.value } : trip,
      );
      await this.#queryClient.invalidateQueries({ queryKey: tripQueryKeys.all });
    },
    onError: (error) =>
      this.scheduleError.set(httpErrorMessage(error, this.text.scheduleSaveError)),
  }));

  readonly busy = computed(() => this.scheduleMutation.isPending());

  readonly dispatchTable = injectTable(() => ({
    features,
    columns: this.columns(),
    data: this.data()?.items ?? [],
    manualSorting: true,
    enableMultiSort: false,
    enableSortingRemoval: false,
    sortDescFirst: false,
    state: {
      sorting: [
        {
          id: Object.keys(sortColumns).find((key) => sortColumns[key] === this.sortBy())!,
          desc: this.sortDirection() === 'Descending',
        },
      ],
    },
    onSortingChange: (updater) => {
      const current = [
        {
          id: Object.keys(sortColumns).find((key) => sortColumns[key] === this.sortBy())!,
          desc: this.sortDirection() === 'Descending',
        },
      ];
      const next = typeof updater === 'function' ? updater(current) : updater;
      if (next[0]) {
        this.sortBy.set(sortColumns[next[0].id]);
        this.sortDirection.set(next[0].desc ? 'Descending' : 'Ascending');
        this.page.set(1);
      }
    },
  }));

  setServiceDate(date: Date | null): void {
    if (date) {
      this.serviceDate.set(toServiceDate(date));
      this.endDate.set(toServiceDate(date));
      this.dateScope.set('day');
      this.page.set(1);
    }
  }

  moveServiceDate(days: number): void {
    const start = this.selectedServiceDate();
    const delta = this.dateScope() === 'week' ? days * 7 : days;
    start.setDate(start.getDate() + delta);
    this.serviceDate.set(toServiceDate(start));
    if (this.dateScope() === 'week') {
      const end = new Date(start);
      end.setDate(end.getDate() + 6);
      this.endDate.set(toServiceDate(end));
    } else this.endDate.set(this.serviceDate());
    this.page.set(1);
  }

  goToToday(): void {
    this.serviceDate.set(toServiceDate(new Date()));
    this.endDate.set(this.serviceDate());
    this.dateScope.set('day');
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
    this.dateScope.set('week');
    this.sortBy.set('ScheduledPickupTime');
    this.sortDirection.set('Ascending');
    this.page.set(1);
  }

  setTomorrow(): void {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    this.serviceDate.set(toServiceDate(tomorrow));
    this.endDate.set(this.serviceDate());
    this.dateScope.set('day');
    this.page.set(1);
  }

  setSearch(value: string): void {
    this.search.set(value);
    clearTimeout(this.#searchTimer);
    const apply = () => {
      this.debouncedSearch.set(value.trim());
      this.page.set(1);
    };
    if (!value.trim()) apply();
    else this.#searchTimer = setTimeout(apply, 300);
  }

  clearFilters(): void {
    this.setSearch('');
    this.needsAttention.set(false);
    this.page.set(1);
  }

  startEditing(trip: AllTripsTrip): void {
    if (this.busy()) return;
    this.editingTripId.set(trip.id);
    this.focusPickup(trip.id, true);
    this.draftTime.set(trip.scheduledPickupTime?.slice(0, 5) ?? '');
    this.scheduleError.set('');
  }

  cancelEditing(): void {
    if (!this.busy()) {
      const id = this.editingTripId();
      this.editingTripId.set(null);
      this.scheduleError.set('');
      if (id) this.focusPickup(id);
    }
  }

  private focusPickup(id: string, editing = false): void {
    afterNextRender(
      () => {
        const cell = Array.from(
          this.#element.nativeElement.querySelectorAll<HTMLElement>('[data-pickup-id]'),
        ).find((element) => element.dataset['pickupId'] === id);
        cell?.querySelector<HTMLElement>(editing ? 'input' : 'button')?.focus();
      },
      { injector: this.#injector },
    );
  }

  readonly formatTime = formatTime;
  readonly formatMobility = formatMobility;
  readonly formatTripDate = (value: string) => serviceDateFormatter.format(toLocalDate(value));

  toggleAttention(): void {
    this.needsAttention.update((value) => !value);
    this.page.set(1);
  }

  setPage(page: number): void {
    this.page.set(page);
  }

  openTrip(trip: AllTripsTrip, sheet: { open: () => void }): void {
    this.selectedTrip.set(trip);
    sheet.open();
  }

  readonly attentionReasons = attentionReasons;
}
