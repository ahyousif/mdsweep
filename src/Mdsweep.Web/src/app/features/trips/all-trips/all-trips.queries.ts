import { AllTripsApi } from './all-trips.api';

export const tripQueryKeys = {
  all: ['trips'] as const,
  serviceDate: (serviceDate: string) => [...tripQueryKeys.all, 'service-date', serviceDate] as const,
};

export const allTripsQueryOptions = (api: AllTripsApi, serviceDate: string) => ({
  queryKey: tripQueryKeys.serviceDate(serviceDate),
  queryFn: () => api.getTrips(serviceDate),
  enabled: serviceDate.length > 0,
});
