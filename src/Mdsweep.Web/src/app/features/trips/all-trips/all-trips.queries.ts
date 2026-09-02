import { AllTripsApi } from './all-trips.api';

export const tripQueryKeys = {
  all: ['trips'] as const,
  conflicts: () => [...tripQueryKeys.all, 'driver-conflicts'] as const,
  serviceDate: (serviceDate: string) => [...tripQueryKeys.all, 'service-date', serviceDate] as const,
  assignedToMe: (serviceDate: string) => [...tripQueryKeys.all, 'assigned-to-me', serviceDate] as const,
};

export const allTripsQueryOptions = (api: AllTripsApi, serviceDate: string) => ({
  queryKey: tripQueryKeys.serviceDate(serviceDate),
  queryFn: () => api.getServiceDay(serviceDate),
  enabled: serviceDate.length > 0,
});
