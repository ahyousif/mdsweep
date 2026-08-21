import { HttpClient } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { uiText } from './ui-text';

type PreviewRow = {
  tripNumber: string;
  disposition: 'Ready' | 'Warning' | 'Blocked';
  brokerChange: 'New' | 'BrokerChanged' | 'Unchanged' | 'Blocked';
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

@Component({
  selector: 'app-root',
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  private readonly http = inject(HttpClient);
  protected readonly text = uiText;
  protected email = 'dispatcher@example.test';
  protected password = '';
  protected readonly signedIn = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal('');
  protected readonly preview = signal<Preview | null>(null);
  protected readonly trips = signal<Trip[]>([]);
  protected readonly serviceDate = signal('');
  protected countBrokerChanges(change: PreviewRow['brokerChange']): number {
    return this.preview()?.rows.filter((row) => row.brokerChange === change).length ?? 0;
  }

  protected readonly journeys = () => {
    const grouped = new Map<string, Trip[]>();
    for (const trip of this.trips()) grouped.set(trip.journeyKey, [...(grouped.get(trip.journeyKey) ?? []), trip]);
    return [...grouped.entries()].map(([journeyKey, trips]) => ({ journeyKey, trips }));
  };

  protected signIn(): void {
    this.busy.set(true);
    this.error.set('');
    this.http.post('/api/auth/login', { email: this.email, password: this.password }).subscribe({
      next: () => { this.signedIn.set(true); this.busy.set(false); },
      error: (response) => { this.error.set(response.error?.message ?? 'Unable to sign in.'); this.busy.set(false); },
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
}
