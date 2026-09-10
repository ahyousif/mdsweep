import { keepPreviousData } from '@tanstack/angular-query-experimental';

import { TripsQuery } from './trips-types';
import { TripsApi } from './trips.api';

export const tripQueryKeys = {
  all: ['trips'] as const,
  list: (query: TripsQuery) => [...tripQueryKeys.all, 'list', query] as const,
};

export const tripsQueryOptions = (api: TripsApi, query: TripsQuery) => ({
  placeholderData: keepPreviousData,
  queryKey: tripQueryKeys.list(query),
  queryFn: () => api.getTrips(query),
  enabled: query.startDate.length > 0 && query.endDate.length > 0,
});
