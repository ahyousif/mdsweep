import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmCheckbox } from '@spartan-ng/helm/checkbox';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import {
  HlmNativeSelect,
  HlmNativeSelectOption,
} from '@spartan-ng/helm/native-select';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import {
  injectMutation,
  injectQuery,
  injectQueryClient,
} from '@tanstack/angular-query-experimental';
import { httpErrorMessage } from '../../core/api/http-error-message';
import { ProviderContext } from '../../core/auth/auth-session.service';
import {
  DriverActionQueue,
  DriverEvent,
} from './data-access/driver-action-queue';
import {
  DriverTrip,
  DriverWorkApi,
  RecordDriverEvent,
} from './data-access/driver-work.api';

type RecordVariables = { trip: DriverTrip; event: RecordDriverEvent };
type CorrectionVariables = {
  tripNumber: string;
  deviceCapturedAt: string;
  reason: string;
};

@Component({
  selector: 'app-driver-work-page',
  imports: [
    HlmBadge,
    HlmButton,
    HlmCheckbox,
    HlmInput,
    HlmLabel,
    HlmNativeSelect,
    HlmNativeSelectOption,
    HlmSpinner,
    ...HlmAlertImports,
    ...HlmCardImports,
  ],
  templateUrl: './driver-work-page.html',
})
export class DriverWorkPage {
  private readonly api = inject(DriverWorkApi);
  private readonly queryClient = injectQueryClient();
  private readonly driverActionQueue = inject(DriverActionQueue);

  protected readonly error = signal('');
  protected readonly queuedDriverActions = this.driverActionQueue.actions;

  protected readonly tripsQuery = injectQuery(() => ({
    queryKey: ['driver-work', 'trips'],
    queryFn: async () => {
      try {
        const trips = await this.api.getTrips();
        localStorage.setItem(this.storageKey(), JSON.stringify(trips));
        return trips;
      } catch (error) {
        const cached = localStorage.getItem(this.storageKey());
        if (cached) return JSON.parse(cached) as DriverTrip[];
        throw error;
      }
    },
  }));

  protected readonly recordMutation = injectMutation(() => ({
    mutationFn: ({ trip, event }: RecordVariables) =>
      this.api.recordEvent(trip.tripNumber, event),
    onSuccess: () =>
      this.queryClient.invalidateQueries({ queryKey: ['driver-work', 'trips'] }),
    onError: (error, { trip, event }) => {
      if (error instanceof HttpErrorResponse && error.status === 0) {
        this.driverActionQueue.enqueue({ tripNumber: trip.tripNumber, event });
        this.queryClient.setQueryData<DriverTrip[]>(
          ['driver-work', 'trips'],
          (trips = []) =>
            trips.map((current) =>
              current.tripNumber === trip.tripNumber
                ? {
                    ...current,
                    lastEventType: event.type,
                    nextAction: this.nextAction(event.type),
                  }
                : current,
            ),
        );
        this.error.set('Waiting to sync. This action is safely stored on this device.');
        return;
      }

      this.error.set(
        httpErrorMessage(error, 'This trip action could not be recorded.'),
      );
    },
  }));

  protected readonly correctionMutation = injectMutation(() => ({
    mutationFn: ({ tripNumber, deviceCapturedAt, reason }: CorrectionVariables) =>
      this.api.correctLatestEvent(tripNumber, deviceCapturedAt, reason),
    onSuccess: () =>
      this.error.set('Correction saved. The original timestamp remains in history.'),
    onError: (error) =>
      this.error.set(
        httpErrorMessage(
          error,
          error instanceof Error
            ? error.message
            : 'This event can no longer be corrected by a Driver.',
        ),
      ),
  }));

  protected navigationUrl(address: string, city: string): string {
    return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(`${address}, ${city}`)}`;
  }

  protected recordDriverEvent(
    trip: DriverTrip,
    type: DriverEvent,
    outcomeReason?: string | null,
    note?: string,
  ): void {
    this.error.set('');
    this.recordMutation.mutate({
      trip,
      event: {
        type,
        deviceCapturedAt: new Date().toISOString(),
        tripLogSigned: type === 'DroppedOff' ? !!trip.tripLogSigned : null,
        outcomeReason: type === 'CouldNotComplete' ? outcomeReason ?? null : null,
        note: type === 'CouldNotComplete' ? note?.trim() || null : null,
      },
    });
  }

  protected setTripLogSigned(tripNumber: string, signed: boolean): void {
    this.queryClient.setQueryData<DriverTrip[]>(
      ['driver-work', 'trips'],
      (trips = []) =>
        trips.map((trip) =>
          trip.tripNumber === tripNumber ? { ...trip, tripLogSigned: signed } : trip,
        ),
    );
  }

  protected correctLatestEvent(trip: DriverTrip): void {
    const reason = window.prompt('Why is this timestamp being corrected?');
    if (!reason?.trim()) return;
    const capturedAt = window.prompt(
      'Correct device time (ISO 8601)',
      new Date().toISOString(),
    );
    if (!capturedAt || Number.isNaN(Date.parse(capturedAt))) {
      this.error.set('Enter a valid timestamp to correct the event.');
      return;
    }

    this.correctionMutation.mutate({
      tripNumber: trip.tripNumber,
      deviceCapturedAt: capturedAt,
      reason: reason.trim(),
    });
  }

  private storageKey(): string {
    const context = this.queryClient.getQueryData<ProviderContext>(['session']);
    return `mdsweep.driver-trips.${context?.providerId ?? 'unknown'}.${context?.appUserId ?? 'unknown'}`;
  }

  private nextAction(event: DriverEvent): DriverEvent | null {
    return (
      {
        ArrivedAtPickup: 'PickedUp',
        PickedUp: 'ArrivedAtDropOff',
        ArrivedAtDropOff: 'DroppedOff',
      } as Partial<Record<DriverEvent, DriverEvent>>
    )[event] ?? null;
  }
}
