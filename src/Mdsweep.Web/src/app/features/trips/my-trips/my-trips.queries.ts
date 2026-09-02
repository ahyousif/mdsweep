import { MyTripsApi } from './my-trips.api';
import { tripQueryKeys } from '../all-trips/all-trips.queries';

export function myTripsQueryOptions(api: MyTripsApi, serviceDate: string, storageKey: string) {
  return {
    queryKey: tripQueryKeys.assignedToMe(serviceDate),
    queryFn: async () => {
      try {
        const trips = await api.getTrips(serviceDate);
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
