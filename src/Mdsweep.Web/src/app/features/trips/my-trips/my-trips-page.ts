import { Component, inject, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { HlmCheckbox } from '@spartan-ng/helm/checkbox';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmLabel } from '@spartan-ng/helm/label';
import { HlmNativeSelect, HlmNativeSelectOption } from '@spartan-ng/helm/native-select';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import {
  injectMutation,
  injectQuery,
  injectQueryClient,
} from '@tanstack/angular-query-experimental';
import { httpErrorMessage } from '../../../core/api/http-error-message';
import { ApplicationError } from '../../../core/errors/application-error';
import { ProviderContext } from '../../../core/auth/auth-session.service';
import { OfflineActionQueue, DriverEvent } from './offline-action-queue';
import { MyTrip, MyTripsApi, RecordDriverEvent } from './my-trips.api';
import { tripQueryKeys } from '../all-trips/all-trips.queries';
import { myTripsQueryOptions } from './my-trips.queries';

type RecordVariables = { trip: MyTrip; event: RecordDriverEvent };
@Component({
  selector: 'app-my-trips-page',
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
  templateUrl: './my-trips-page.html',
})
export class MyTripsPage {
  private readonly api = inject(MyTripsApi);
  private readonly queryClient = injectQueryClient();
  private readonly driverActionQueue = inject(OfflineActionQueue);

  protected readonly error = signal('');
  protected readonly serviceDate = new Date().toISOString().slice(0, 10);
  protected readonly queuedDriverActions = this.driverActionQueue.actions;

  protected readonly tripsQuery = injectQuery(() =>
    myTripsQueryOptions(this.api, this.serviceDate, this.storageKey()),
  );

  protected readonly recordMutation = injectMutation(() => ({
    mutationFn: ({ trip, event }: RecordVariables) => this.api.recordEvent(trip.tripNumber, event),
    onSuccess: () => this.queryClient.invalidateQueries({ queryKey: tripQueryKeys.all }),
    onError: (error, { trip, event }) => {
      if (error instanceof ApplicationError && error.status === 0) {
        this.driverActionQueue.enqueue({ tripNumber: trip.tripNumber, event });
        this.queryClient.setQueryData<MyTrip[]>(tripQueryKeys.assignedToMe(new Date().toISOString().slice(0, 10)), (trips = []) =>
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

      this.error.set(httpErrorMessage(error, 'This trip action could not be recorded.'));
    },
  }));

  protected navigationUrl(address: string, city: string): string {
    return `https://www.google.com/maps/dir/?api=1&destination=${encodeURIComponent(`${address}, ${city}`)}`;
  }

  protected recordDriverEvent(
    trip: MyTrip,
    type: DriverEvent,
    outcomeReason?: string | null,
    note?: string,
  ): void {
    this.error.set('');
    this.recordMutation.mutate({
      trip,
      event: {
        clientEventId: crypto.randomUUID(),
        type,
        deviceCapturedAt: new Date().toISOString(),
        tripLogSigned: type === 'DroppedOff' ? !!trip.tripLogSigned : null,
        outcomeReason: type === 'CouldNotComplete' ? (outcomeReason ?? null) : null,
        note: type === 'CouldNotComplete' ? note?.trim() || null : null,
      },
    });
  }

  protected setTripLogSigned(tripNumber: string, signed: boolean): void {
    this.queryClient.setQueryData<MyTrip[]>(tripQueryKeys.assignedToMe(new Date().toISOString().slice(0, 10)), (trips = []) =>
      trips.map((trip) =>
        trip.tripNumber === tripNumber ? { ...trip, tripLogSigned: signed } : trip,
      ),
    );
  }

  private storageKey(): string {
    const context = this.queryClient.getQueryData<ProviderContext>(['auth', 'session']);
    return `mdsweep.driver-trips.${context?.providerId ?? 'unknown'}.${context?.appUserId ?? 'unknown'}`;
  }

  private nextAction(event: DriverEvent): DriverEvent | null {
    return (
      (
        {
          ArrivedAtPickup: 'PickedUp',
          PickedUp: 'ArrivedAtDropOff',
          ArrivedAtDropOff: 'DroppedOff',
        } as Partial<Record<DriverEvent, DriverEvent>>
      )[event] ?? null
    );
  }
}
