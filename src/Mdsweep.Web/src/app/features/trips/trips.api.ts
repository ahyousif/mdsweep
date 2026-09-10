import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiClient } from '@app/core/api/api-client';
import { PagedResponse } from '@app/core/api/paged-response';

import { Trip, TripsQuery } from './trips-types';

@Injectable({ providedIn: 'root' })
export class TripsApi {
  readonly #api = inject(ApiClient);

  getTrips(query: TripsQuery): Promise<PagedResponse<Trip>> {
    const params: Record<string, string | number | boolean> = {
      startDate: query.startDate,
      endDate: query.endDate,
      page: query.page ?? 1,
      pageSize: query.pageSize ?? 100,
    };

    if (query.search) {
      params['search'] = query.search;
    }

    if (query.brokerStatus) {
      params['brokerStatus'] = query.brokerStatus;
    }

    if (query.isWillCall !== undefined) {
      params['isWillCall'] = query.isWillCall;
    }

    return firstValueFrom(
      this.#api.http.get<PagedResponse<Trip>>(this.#api.url('trips'), {
        params,
      }),
    );
  }
}
