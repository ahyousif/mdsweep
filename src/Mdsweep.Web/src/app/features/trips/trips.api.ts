import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiClient } from '@app/core/api/api-client';
import { PagedResponse } from '@app/core/api/paged-response';

import { Trip, TripsQuery } from './trips-types';

@Injectable({ providedIn: 'root' })
export class TripsApi {
  readonly #api = inject(ApiClient);

  setScheduledPickupTime(id: string, scheduledPickupTime: string | null): Promise<unknown> {
    return firstValueFrom(
      this.#api.http.put(this.#api.url(`trips/${id}/scheduled-pickup-time`), {
        scheduledPickupTime,
      }),
    );
  }

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

  async getAllTrips(query: TripsQuery): Promise<Trip[]> {
    // Journey grouping must see every Trip in the bounded date window. Search stays client-side so
    // matching one leg cannot hide its Journey siblings; the API's 100-row safety cap still applies
    // to each request.
    const unfilteredQuery = { ...query, search: undefined, page: 1, pageSize: 100 };
    const firstPage = await this.getTrips(unfilteredQuery);
    const remainingPages = await Promise.all(
      Array.from({ length: Math.max(0, firstPage.totalPages - 1) }, (_, index) =>
        this.getTrips({ ...unfilteredQuery, page: index + 2 }),
      ),
    );

    const tripsById = new Map(firstPage.items.map((trip) => [trip.id, trip]));
    for (const page of remainingPages) {
      for (const trip of page.items) {
        tripsById.set(trip.id, trip);
      }
    }

    if (tripsById.size !== firstPage.totalCount) {
      // Concurrent imports/edits can shift page boundaries. Never show a partially grouped
      // Journey as though its membership were complete; the query's retry/recovery UI applies.
      throw new Error('The Trip list changed while loading; retry to load complete Journeys.');
    }

    return Array.from(tripsById.values());
  }
}
