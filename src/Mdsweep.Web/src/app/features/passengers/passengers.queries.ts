import { PassengersApi, type PassengersQuery } from './passengers.api';

export const passengerQueryKeys = {
  all: ['passengers'] as const,
  list: (query: PassengersQuery) => [...passengerQueryKeys.all, 'list', query] as const,
  detail: (id: string) => [...passengerQueryKeys.all, 'detail', id] as const,
};

export const passengersQueryOptions = (api: PassengersApi, query: PassengersQuery) => ({
  queryKey: passengerQueryKeys.list(query),
  queryFn: () => api.list(query),
});

export const passengerDetailQueryOptions = (api: PassengersApi, id: string | null) => ({
  queryKey: passengerQueryKeys.detail(id ?? 'none'),
  queryFn: () => api.get(id!),
  enabled: id !== null,
});
