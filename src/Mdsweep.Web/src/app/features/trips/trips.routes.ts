import { Routes } from '@angular/router';

const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./all-trips/all-trips-page'),
  },
  {
    path: 'import',
    loadComponent: () => import('./trip-import/trip-import-page'),
  },
];

export default routes;
