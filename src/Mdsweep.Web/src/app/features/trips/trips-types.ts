export type Address = {
  address: string;
  city: string;
  state: string | null;
  zip: string | null;
};

export type TripsQuery = {
  startDate: string;
  endDate: string;
  search?: string;
  brokerStatus?: string;
  isWillCall?: boolean;
  page?: number;
  pageSize?: number;
};

export type TripDirection = 'To' | 'From';
export type TripLifecycle = 'scheduled' | 'inProgress' | 'completed';

export type TripAssignment = {
  driverName: string;
  vehicleName: string | null;
};

export type Trip = {
  id: string;
  journeyId: string;
  brokerTripNumber: string;

  passengerFirstName: string;
  passengerLastName: string;
  memberId: string | null;

  serviceDate: string;
  direction: TripDirection;
  brokerStatus: string | null;
  isWillCall: boolean;
  // The current Trip API omits execution and assignment data. Keep these optional until it
  // exposes the Trip-owned values; never infer them from pickup scheduling or Journey state.
  lifecycleStatus?: TripLifecycle | null;
  assignment?: TripAssignment | null;

  passengerType: string | null;
  specialNeeds: string | null;

  tripCost: number | null;
  tripMileage: number | null;

  appointmentTime: string | null;
  returnPickupTime: string | null;
  scheduledPickupTime: string | null;
  calculatedPickupTime: string | null;
  manualPickupTime: string | null;

  estimatedTravelMinutes: number | null;
  estimatedDistanceMeters: number | null;

  pickup: Address;
  dropoff: Address;
};
