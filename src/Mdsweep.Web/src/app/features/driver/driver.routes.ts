import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./trips/driver-trips-page').then((m) => m.DriverTripsPage),
  },
];
