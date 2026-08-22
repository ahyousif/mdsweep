import { HttpClient } from '@angular/common/http';
import { Component, inject, OnInit, signal } from '@angular/core';
import { uiText } from './ui-text';
import { DriverActionQueue, DriverEvent } from './driver-action-queue';

type PreviewRow = {
  tripNumber: string;
  disposition: 'Ready' | 'Warning' | 'Blocked';
  brokerChange: 'New' | 'BrokerChanged' | 'Unchanged' | 'Blocked';
  hasProviderOverrides: boolean;
  isActive: boolean;
  messages: string[];
};
type Preview = { previewId: string; ready: number; warning: number; blocked: number; serviceDates: string[]; rows: PreviewRow[] };
type Trip = {
  tripNumber: string;
  journeyKey: string;
  memberName: string;
  pickupAddress: string;
  pickupCity: string;
  deliveryAddress: string;
  deliveryCity: string;
  passengerType: string;
  vehicleType: string;
  brokerStatus: string;
  appointmentTime: string;
  scheduledPickupTime: string | null;
  isWillCall: boolean;
  isActive: boolean;
};
type ProviderContext = { appUserId: string; providerId: string; role: 'Dispatcher' | 'Driver' };
type DriverTrip = {
  tripNumber: string; appointmentDate: string; appointmentTime: string; memberName: string;
  passengerType: string; vehicleType: string; pickupAddress: string; pickupCity: string;
  deliveryAddress: string; deliveryCity: string; nextAction: DriverEvent | null; lastEventType: DriverEvent | null;
  passengerPhone: string | null;
  tripLogSigned?: boolean;
};
type DriverHistoryEvent = { id: string; deviceCapturedAt: string };

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly driverActionQueue = inject(DriverActionQueue);
  protected readonly text = uiText;
  protected readonly signedIn = signal(false);
  protected readonly role = signal<ProviderContext['role'] | null>(null);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly preview = signal<Preview | null>(null);
  protected readonly trips = signal<Trip[]>([]);
  protected readonly serviceDate = signal('');
  protected readonly driverTrips = signal<DriverTrip[]>([]);
  protected readonly queuedDriverActions = this.driverActionQueue.actions;
  protected readonly conflicts = signal<{ tripNumber: string; reason: string; deviceCapturedAt: string }[]>([]);
  private driverStorageKey = '';
  ngOnInit(): void {
    this.http.get<ProviderContext[]>('/api/auth/me').subscribe({
      next: (contexts) => {
        if (contexts.length !== 1) {
          this.error.set('Choose a Provider before using MDSweep.');
          return;
        }
        this.http.post('/api/auth/provider-context', { providerId: contexts[0].providerId }).subscribe({
          next: () => this.http.get('/api/auth/antiforgery').subscribe({
            next: () => {
              this.role.set(contexts[0].role);
              this.driverStorageKey = `mdsweep.driver-trips.${contexts[0].providerId}.${contexts[0].appUserId}`;
              this.signedIn.set(true);
              if (contexts[0].role === 'Driver') this.loadDriverTrips(); else this.loadConflicts();
            },
            error: () => this.error.set('Unable to establish the application session.'),
          }),
          error: () => this.error.set('Unable to select the Provider context.'),
        });
      },
      error: () => this.signedIn.set(false),
    });
  }
  protected countBrokerChanges(change: PreviewRow['brokerChange']): number {
    return this.preview()?.rows.filter((row) => row.brokerChange === change).length ?? 0;
  }
  protected countProviderOverrides(): number {
    return this.preview()?.rows.filter((row) => row.hasProviderOverrides).length ?? 0;
  }
  protected countInactive(): number {
    return this.preview()?.rows.filter((row) => !row.isActive && row.disposition !== 'Blocked').length ?? 0;
  }

  protected readonly journeys = () => {
    const grouped = new Map<string, Trip[]>();
    for (const trip of this.trips()) grouped.set(trip.journeyKey, [...(grouped.get(trip.journeyKey) ?? []), trip]);
    return [...grouped.entries()].map(([journeyKey, trips]) => ({ journeyKey, trips }));
  };

  protected signIn(): void {
    window.location.assign('/api/auth/login');
  }

  protected navigationUrl(address: string, city: string): string {
    return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(`${address}, ${city}`)}`;
  }

  protected recordDriverEvent(trip: DriverTrip, type: DriverEvent, outcomeReason?: string, note?: string): void {
    this.busy.set(true);
    this.error.set('');
    const event = {
      type,
      deviceCapturedAt: new Date().toISOString(),
      tripLogSigned: type === 'DroppedOff' ? !!trip.tripLogSigned : null,
      outcomeReason: type === 'CouldNotComplete' ? outcomeReason ?? null : null,
      note: type === 'CouldNotComplete' ? note?.trim() || null : null,
    };
    this.http.post(`/api/driver-work/trips/${encodeURIComponent(trip.tripNumber)}/events`, event).subscribe({
      next: () => this.loadDriverTrips(),
      error: (response) => {
        if (!response.status) {
          this.driverActionQueue.enqueue({ tripNumber: trip.tripNumber, event });
          this.driverTrips.update((trips) => trips.map((current) => current.tripNumber === trip.tripNumber ? { ...current, lastEventType: type, nextAction: this.nextAction(type) } : current));
          this.error.set('Waiting to sync. This action is safely stored on this device.');
        } else this.error.set(response.error?.message ?? 'This trip action could not be recorded.');
        this.busy.set(false);
      },
    });
  }

  protected setTripLogSigned(tripNumber: string, signed: boolean): void {
    this.driverTrips.update((trips) => trips.map((trip) => trip.tripNumber === tripNumber ? { ...trip, tripLogSigned: signed } : trip));
  }

  protected correctLatestEvent(trip: DriverTrip): void {
    const reason = window.prompt('Why is this timestamp being corrected?');
    if (!reason?.trim()) return;
    const capturedAt = window.prompt('Correct device time (ISO 8601)', new Date().toISOString());
    if (!capturedAt || Number.isNaN(Date.parse(capturedAt))) { this.error.set('Enter a valid timestamp to correct the event.'); return; }
    this.busy.set(true);
    this.http.get<DriverHistoryEvent[]>(`/api/driver-work/trips/${encodeURIComponent(trip.tripNumber)}/history`).subscribe({
      next: (history) => {
        const event = history.at(-1);
        if (!event) { this.error.set('There is no event to correct.'); this.busy.set(false); return; }
        this.http.post(`/api/driver-work/trips/${encodeURIComponent(trip.tripNumber)}/events/${event.id}/corrections`, { deviceCapturedAt: capturedAt, reason: reason.trim() }).subscribe({
          next: () => { this.error.set('Correction saved. The original timestamp remains in history.'); this.busy.set(false); },
          error: (response) => { this.error.set(response.error?.message ?? 'This event can no longer be corrected by a Driver.'); this.busy.set(false); },
        });
      },
      error: () => { this.error.set('Event history could not be loaded.'); this.busy.set(false); },
    });
  }

  protected chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    const form = new FormData();
    form.append('file', file);
    this.busy.set(true);
    this.error.set('');
    this.http.post<Preview>('/api/manifest-imports/preview', form).subscribe({
      next: (preview) => {
        this.preview.set(preview);
        this.serviceDate.set(preview.serviceDates[0] ?? '');
        this.busy.set(false);
      },
      error: (response) => { this.error.set(response.error?.message ?? 'Unable to check this Manifest.'); this.busy.set(false); },
    });
  }

  protected apply(): void {
    const preview = this.preview();
    if (!preview) return;
    this.busy.set(true);
    this.http.post(`/api/manifest-imports/${preview.previewId}/apply`, {}).subscribe({
      next: () => this.loadServiceDay(),
      error: () => { this.error.set('Unable to import this Manifest.'); this.busy.set(false); },
    });
  }

  protected setScheduledPickupTime(trip: Trip, value: string): void {
    if (!value) return;
    this.busy.set(true);
    this.error.set('');
    this.http.put(`/api/trips/${encodeURIComponent(trip.tripNumber)}/scheduled-pickup-time`, {
      scheduledPickupTime: value.length === 5 ? `${value}:00` : value,
    }).subscribe({
      next: () => this.loadServiceDay(),
      error: (response) => {
        this.error.set(response.error?.message ?? this.text.scheduleSaveError);
        this.busy.set(false);
      },
    });
  }

  private loadServiceDay(): void {
    const serviceDate = this.serviceDate();
    if (!serviceDate) { this.error.set('The Manifest has no valid service date.'); this.busy.set(false); return; }
    this.http.get<Trip[]>(`/api/service-days/${serviceDate}/trips`).subscribe({
      next: (trips) => { this.trips.set(trips); this.busy.set(false); },
      error: () => { this.error.set('Trips were imported, but the service day could not be loaded.'); this.busy.set(false); },
    });
  }

  private loadDriverTrips(): void {
    this.http.get<DriverTrip[]>('/api/driver-work/trips').subscribe({
      next: (trips) => { this.driverTrips.set(trips); localStorage.setItem(this.driverStorageKey, JSON.stringify(trips)); this.busy.set(false); },
      error: () => { const cached = localStorage.getItem(this.driverStorageKey); if (cached) this.driverTrips.set(JSON.parse(cached)); else this.error.set('Your assigned trips could not be loaded.'); this.busy.set(false); },
    });
  }

  private loadConflicts(): void { this.http.get<{ tripNumber: string; reason: string; deviceCapturedAt: string }[]>('/api/driver-work/conflicts').subscribe({ next: (items) => this.conflicts.set(items) }); }

  private nextAction(event: DriverEvent): DriverEvent | null {
    return ({ ArrivedAtPickup: 'PickedUp', PickedUp: 'ArrivedAtDropOff', ArrivedAtDropOff: 'DroppedOff' } as Partial<Record<DriverEvent, DriverEvent>>)[event] ?? null;
  }
}
