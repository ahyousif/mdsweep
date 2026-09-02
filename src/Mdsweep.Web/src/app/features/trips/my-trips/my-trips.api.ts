import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { DriverEvent } from './offline-action-queue';

export type MyTrip = {
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
  clientEventId: string;
  type: DriverEvent;
  deviceCapturedAt: string;
  tripLogSigned: boolean | null;
  outcomeReason: string | null;
  note: string | null;
};

@Injectable({ providedIn: 'root' })
export class MyTripsApi {
  private readonly http = inject(HttpClient);

  getTrips(serviceDate: string): Promise<MyTrip[]> {
    return firstValueFrom(
      this.http.get<MyTrip[]>('/api/trips/assigned-to-me', { params: { serviceDate } }),
    );
  }

  recordEvent(tripNumber: string, event: RecordDriverEvent): Promise<void> {
    return firstValueFrom(
      this.http.post<void>(
        `/api/trips/${encodeURIComponent(tripNumber)}/progress-events`,
        event,
      ),
    );
  }

}
