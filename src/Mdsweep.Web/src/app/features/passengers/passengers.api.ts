import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiClient } from '@app/core/api/api-client';
import { PagedResponse } from '@app/core/api/paged-response';

export type Passenger = {
  id: string;
  brokerMemberId: string | null;
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  alternatePhoneNumber: string | null;
  passengerType: string | null;
  specialNeeds: string | null;
  notes: string | null;
  isActive: boolean;
};

export type PassengerDetails = Omit<Passenger, 'id' | 'isActive'>;
export type CreatePassengerDetails = Pick<PassengerDetails, 'brokerMemberId' | 'firstName' | 'lastName'>;

export type PassengersQuery = {
  search?: string;
  page: number;
  pageSize: number;
};

@Injectable({ providedIn: 'root' })
export class PassengersApi {
  readonly #api = inject(ApiClient);

  list(query: PassengersQuery): Promise<PagedResponse<Passenger>> {
    const params: Record<string, string | number> = { page: query.page, pageSize: query.pageSize };
    if (query.search?.trim()) params['search'] = query.search.trim();

    return firstValueFrom(
      this.#api.http.get<PagedResponse<Passenger>>(this.#api.url('passengers'), { params }),
    );
  }

  get(id: string): Promise<Passenger> {
    return firstValueFrom(this.#api.http.get<Passenger>(this.#api.url(`passengers/${id}`)));
  }

  create(details: CreatePassengerDetails): Promise<Passenger> {
    return firstValueFrom(this.#api.http.post<Passenger>(this.#api.url('passengers'), {
      brokerMemberId: details.brokerMemberId,
      firstName: details.firstName,
      lastName: details.lastName,
    }));
  }

  update(id: string, details: PassengerDetails): Promise<void> {
    return firstValueFrom(this.#api.http.put<void>(this.#api.url(`passengers/${id}`), details));
  }

  disable(id: string): Promise<void> {
    return firstValueFrom(this.#api.http.post<void>(this.#api.url(`passengers/${id}/disable`), {}));
  }

  enable(id: string): Promise<void> {
    return firstValueFrom(this.#api.http.post<void>(this.#api.url(`passengers/${id}/enable`), {}));
  }
}
