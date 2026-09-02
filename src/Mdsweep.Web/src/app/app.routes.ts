import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: 'dispatch',
    loadChildren: () =>
      import('./features/dispatcher/dispatcher.routes').then((module) => module.routes),
  },
  {
    path: 'driver',
    loadChildren: () => import('./features/driver/driver.routes').then((module) => module.routes),
  },
  { path: '', pathMatch: 'full', redirectTo: 'dispatch' },
  { path: '**', redirectTo: 'dispatch' },
];
