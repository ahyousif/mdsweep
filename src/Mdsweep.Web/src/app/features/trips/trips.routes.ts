import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./all-trips/all-trips-page').then((m) => m.AllTripsPage),
  },
  {
    path: 'mine',
    loadComponent: () => import('./my-trips/my-trips-page').then((m) => m.MyTripsPage),
  },
  {
    path: 'import',
    loadComponent: () => import('./trip-import/trip-import-page').then((m) => m.TripImportPage),
  },
];
