import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./all-trips/all-trips-page').then((m) => m.AllTripsPage),
  },
  {
    path: 'import',
    loadComponent: () => import('./trip-import/trip-import-page').then((m) => m.TripImportPage),
  },
];
