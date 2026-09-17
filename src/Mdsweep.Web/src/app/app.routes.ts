import { Routes } from '@angular/router';
import { usersGuard } from './features/users/users.guard';
import { vehiclesGuard } from './features/vehicles/vehicles.guard';

export const routes: Routes = [
  {
    path: 'vehicles',
    canActivate: [vehiclesGuard],
    loadComponent: () => import('./features/vehicles/vehicles-page'),
  },
  {
    path: 'invitations/accept',
    loadComponent: () => import('./features/users/invitation-welcome'),
  },
  {
    path: 'users',
    canActivate: [usersGuard],
    loadComponent: () => import('./features/users/users-page'),
  },
  {
    path: 'trips',
    loadChildren: () => import('./features/trips/trips.routes'),
  },
  { path: '', pathMatch: 'full', redirectTo: 'trips' },
  { path: '**', redirectTo: 'trips' },
];
