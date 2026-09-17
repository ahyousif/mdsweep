import { VehiclesApi } from './vehicles.api';
export const vehicleQueryKeys = {
  all: ['vehicles'] as const,
  list: ['vehicles', 'list'] as const,
};
export const vehiclesQueryOptions = (api: VehiclesApi) => ({
  queryKey: vehicleQueryKeys.list,
  queryFn: () => api.list(),
});
