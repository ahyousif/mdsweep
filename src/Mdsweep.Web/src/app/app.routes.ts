import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dispatch',
    loadComponent: () =>
      import('./features/dispatch/dispatch-page').then((module) => module.DispatchPage),
  },
  {
    path: 'driver',
    loadComponent: () =>
      import('./features/driver-work/driver-work-page').then(
        (module) => module.DriverWorkPage,
      ),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dispatch' },
  { path: '**', redirectTo: 'dispatch' },
];
