import { keepPreviousData } from '@tanstack/angular-query-experimental';
import { AllTripsApi, TripsQuery } from './all-trips.api';

export const tripQueryKeys = {
  all: ['trips'] as const,
  workspace: (query: TripsQuery) => [...tripQueryKeys.all, 'workspace', query] as const,
};

export const allTripsQueryOptions = (api: AllTripsApi, query: TripsQuery) => ({
  placeholderData: keepPreviousData,
  queryKey: tripQueryKeys.workspace(query),
  queryFn: () => api.getTrips(query),
  enabled: query.startDate.length > 0 && query.endDate.length > 0,
});
