import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type DispatchTrip = {
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

export type DriverSyncConflict = {
  tripNumber: string;
  reason: string;
  deviceCapturedAt: string;
};

@Injectable({ providedIn: 'root' })
export class DispatchApi {
  private readonly http = inject(HttpClient);

  getServiceDay(serviceDate: string): Promise<DispatchTrip[]> {
    return firstValueFrom(this.http.get<DispatchTrip[]>(`/api/service-days/${serviceDate}/trips`));
  }

  getConflicts(): Promise<DriverSyncConflict[]> {
    return firstValueFrom(this.http.get<DriverSyncConflict[]>('/api/driver-work/conflicts'));
  }

  setScheduledPickupTime(tripNumber: string, value: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`/api/trips/${encodeURIComponent(tripNumber)}/scheduled-pickup-time`, {
        scheduledPickupTime: value.length === 5 ? `${value}:00` : value,
      }),
    );
  }
}
