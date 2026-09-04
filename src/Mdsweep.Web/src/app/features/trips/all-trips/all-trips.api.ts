import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '@app/core/api/api-client';
import { PagedResponse } from '@app/core/api/paged-response';

export type AllTripsTrip = {
  id: string;
  brokerTripNumber: string;
  passengerFirstName: string;
  passengerLastName: string;
  brokerMemberId: string | null;
  serviceDate: string;
  appointmentTime: string | null;
  brokerStatus: string | null;
  isWillCall: boolean;
  mobilityRequirement: 'Ambulatory' | 'Cane' | 'ManualWheelchair' | 'ManualWheelchairCannotTransfer' | 'ElectricWheelchair';
  requiredVehicleCapability: 'StandardTransport' | 'WheelchairAccessible';
  tripCost: number | null;
  tripMileage: number | null;
  scheduledPickupTime: string | null;
  pickupAddress: string;
  pickupCity: string;
  dropoffAddress: string;
  dropoffCity: string;
};

export type TripsQuery = {
  startDate: string;
  endDate: string;
  search?: string;
  needsAttention?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'Ascending' | 'Descending';
};

@Injectable({ providedIn: 'root' })
export class AllTripsApi {
  readonly #api = inject(ApiClient);

  getTrips(query: TripsQuery): Promise<PagedResponse<AllTripsTrip>> {
    const params: Record<string, string | number | boolean> = {
      startDate: query.startDate,
      endDate: query.endDate,
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 50,
      sortBy: query.sortBy ?? 'ScheduledPickupTime',
      sortDirection: query.sortDirection ?? 'Ascending',
    };
    if (query.search) params['search'] = query.search;
    if (query.needsAttention) params['needsAttention'] = true;

    return firstValueFrom(
      this.#api.http.get<PagedResponse<AllTripsTrip>>(this.#api.url('trips'), {
        params,
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
