import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DriverEvent } from '../offline/driver-action-queue.store';

export type DriverTrip = {
  tripNumber: string;
  appointmentDate: string;
  appointmentTime: string;
  memberName: string;
  passengerType: string;
  vehicleType: string;
  pickupAddress: string;
  pickupCity: string;
  deliveryAddress: string;
  deliveryCity: string;
  nextAction: DriverEvent | null;
  lastEventType: DriverEvent | null;
  passengerPhone: string | null;
  tripLogSigned?: boolean;
};

export type RecordDriverEvent = {
  type: DriverEvent;
  deviceCapturedAt: string;
  tripLogSigned: boolean | null;
  outcomeReason: string | null;
  note: string | null;
};

type DriverHistoryEvent = { id: string; deviceCapturedAt: string };

@Injectable({ providedIn: 'root' })
export class DriverTripsApi {
  private readonly http = inject(HttpClient);

  getTrips(): Promise<DriverTrip[]> {
    return firstValueFrom(this.http.get<DriverTrip[]>('/api/driver-work/trips'));
  }

  recordEvent(tripNumber: string, event: RecordDriverEvent): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(
        `/api/driver-work/trips/${encodeURIComponent(tripNumber)}/events`,
        event,
      ),
    );
  }

  async correctLatestEvent(
    tripNumber: string,
    deviceCapturedAt: string,
    reason: string,
  ): Promise<void> {
    const history = await firstValueFrom(
      this.http.get<DriverHistoryEvent[]>(
        `/api/driver-work/trips/${encodeURIComponent(tripNumber)}/history`,
      ),
    );
    const event = history.at(-1);
    if (!event) {
      throw new Error('There is no event to correct.');
    }

    await firstValueFrom(
      this.http.post<void>(
        `/api/driver-work/trips/${encodeURIComponent(tripNumber)}/events/${event.id}/corrections`,
        { deviceCapturedAt, reason },
      ),
    );
  }
}
