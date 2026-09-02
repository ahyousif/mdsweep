import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'trips',
    loadChildren: () => import('./features/trips/trips.routes').then((module) => module.routes),
  },
  { path: '', pathMatch: 'full', redirectTo: 'trips' },
  { path: '**', redirectTo: 'trips' },
];
