import { DispatchApi } from './day-board.api';

export const dispatcherQueryKeys = {
  conflicts: () => ['dispatcher', 'driver-conflicts'] as const,
  serviceDay: (serviceDate: string) => ['dispatcher', 'service-day', serviceDate] as const,
};

export const dispatcherQueryOptions = (api: DispatchApi, serviceDate: string) => ({
  queryKey: dispatcherQueryKeys.serviceDay(serviceDate),
  queryFn: () => api.getServiceDay(serviceDate),
  enabled: serviceDate.length > 0,
});
