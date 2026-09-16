import type { QueryClient } from '@tanstack/query-core';

import { TripsQuery } from './trips-types';
import { TripsApi } from './trips.api';

export const tripQueryKeys = {
  all: ['trips'] as const,

  list: (query: TripsQuery) => [...tripQueryKeys.all, 'list', query] as const,
};

export const tripsQueryOptions = (api: TripsApi, query: TripsQuery) => ({
  queryKey: tripQueryKeys.list(query),
  queryFn: () => api.getAllTrips(query),
  enabled: query.startDate.length > 0 && query.endDate.length > 0,
});

export const scheduledPickupMutationOptions = (api: TripsApi, queryClient: QueryClient) => ({
  mutationFn: ({ id, time }: { id: string; time: string | null }) =>
    api.setScheduledPickupTime(id, time),
  onSuccess: () => queryClient.invalidateQueries({ queryKey: tripQueryKeys.all }),
});
