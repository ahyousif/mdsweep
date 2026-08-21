import { HttpClient } from '@angular/common/http';
import { Injectable, signal } from '@angular/core';

export type DriverEvent = 'ArrivedAtPickup' | 'PickedUp' | 'ArrivedAtDropOff' | 'DroppedOff' | 'CouldNotComplete';

export type QueuedDriverAction = {
  id: string;
  tripNumber: string;
  event: { type: DriverEvent; deviceCapturedAt: string; tripLogSigned: boolean | null; outcomeReason: string | null; note: string | null };
  state: 'WaitingToSync' | 'NeedsAttention';
};

@Injectable({ providedIn: 'root' })
export class DriverActionQueue {
  private readonly key = 'mdsweep.driver-actions';
  readonly actions = signal<QueuedDriverAction[]>(this.read());

  constructor(private readonly http: HttpClient) {
    window.addEventListener('online', () => this.synchronize());
    this.synchronize();
  }

  enqueue(action: Omit<QueuedDriverAction, 'id' | 'state'>): void {
    this.update([...this.actions(), { ...action, id: crypto.randomUUID(), state: 'WaitingToSync' }]);
  }

  synchronize(): void {
    if (!navigator.onLine) return;
    for (const action of this.actions().filter((item) => item.state === 'WaitingToSync')) {
      this.http.post(`/api/driver-work/events/sync`, { tripNumber: action.tripNumber, event: action.event }).subscribe({
        next: () => this.update(this.actions().filter((item) => item.id !== action.id)),
        error: (response) => {
          if (response.status && response.status !== 0) {
            this.update(this.actions().map((item) => item.id === action.id ? { ...item, state: 'NeedsAttention' } : item));
          }
        },
      });
    }
  }

  private read(): QueuedDriverAction[] {
    try { return JSON.parse(localStorage.getItem(this.key) ?? '[]'); } catch { return []; }
  }

  private update(actions: QueuedDriverAction[]): void {
    this.actions.set(actions);
    localStorage.setItem(this.key, JSON.stringify(actions));
  }
}
