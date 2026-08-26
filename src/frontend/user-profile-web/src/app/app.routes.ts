import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: 'register',
    loadComponent: () => import('./features/auth/register/register').then(({ Register }) => Register),
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then(({ Login }) => Login),
  },
  {
    path: 'dashboard',
    loadComponent: () => import('./features/dashboard/dashboard').then(({ Dashboard }) => Dashboard),
    canActivate: [authGuard],
  },
  {
    path: 'profile',
    loadComponent: () =>
      import('./features/profile/profile-placeholder').then(
        ({ ProfilePlaceholder }) => ProfilePlaceholder,
      ),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: 'login' },
];
