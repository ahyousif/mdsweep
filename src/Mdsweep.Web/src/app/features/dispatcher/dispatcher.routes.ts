import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./day-board/day-board-page').then((m) => m.DayBoardPage),
  },
];
