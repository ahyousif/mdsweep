import { DriverTripsApi } from './driver-trips.api';

export const driverQueryKeys = { trips: () => ['driver', 'trips'] as const };

export function driverTripsQueryOptions(api: DriverTripsApi, storageKey: string) {
  return {
    queryKey: driverQueryKeys.trips(),
    queryFn: async () => {
      try {
        const trips = await api.getTrips();
        localStorage.setItem(storageKey, JSON.stringify(trips));
        return trips;
      } catch (error) {
        const cached = localStorage.getItem(storageKey);
        if (cached) return JSON.parse(cached);
        throw error;
      }
    },
  };
}
