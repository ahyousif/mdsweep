import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '../../../core/api/api-client';
import { PagedResponse } from '../../../core/api/paged-response';

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

@Injectable({ providedIn: 'root' })
export class AllTripsApi {
  readonly #api = inject(ApiClient);

  getTrips(serviceDate: string, page = 1, pageSize = 50): Promise<PagedResponse<AllTripsTrip>> {
    return firstValueFrom(
      this.#api.http.get<PagedResponse<AllTripsTrip>>(this.#api.url('trips'), {
        params: { serviceDate, page, pageSize },
      }),
    );
  }

  setScheduledPickupTime(id: string, value: string): Promise<void> {
    return firstValueFrom(
      this.#api.http.put<void>(
        this.#api.url(`trips/${encodeURIComponent(id)}/scheduled-pickup-time`),
        { scheduledPickupTime: value.length === 5 ? `${value}:00` : value },
      ),
    );
  }
}
