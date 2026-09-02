import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type AllTripsTrip = {
  id: string;
  brokerTripNumber: string;
  serviceDate: string;
  appointmentTime: string | null;
  brokerStatus: string | null;
  isWillCall: boolean;
  scheduledPickupTime: string | null;
  pickupAddress: string;
  pickupCity: string;
  dropoffAddress: string;
  dropoffCity: string;
};

export type PagedResponse<T> = {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
};

@Injectable({ providedIn: 'root' })
export class AllTripsApi {
  private readonly http = inject(HttpClient);

  getTrips(serviceDate: string, page = 1, pageSize = 50): Promise<PagedResponse<AllTripsTrip>> {
    return firstValueFrom(
      this.http.get<PagedResponse<AllTripsTrip>>('/api/trips', {
        params: { serviceDate, page, pageSize },
      }),
    );
  }

  setScheduledPickupTime(id: string, value: string): Promise<void> {
    return firstValueFrom(
      this.http.put<void>(`/api/trips/${encodeURIComponent(id)}/scheduled-pickup-time`, {
        scheduledPickupTime: value.length === 5 ? `${value}:00` : value,
      }),
    );
  }
}
