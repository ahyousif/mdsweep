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

export type Trip = {
  id: string;
  brokerTripNumber: string;

  passengerFirstName: string;
  passengerLastName: string;
  memberId: string | null;

  serviceDate: string;
  direction: TripDirection;
  brokerStatus: string | null;
  isWillCall: boolean;

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
