import { Routes } from '@angular/router';
import { LoginPlaceholder } from './features/auth/login/login-placeholder';
import { Register } from './features/auth/register/register';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'register' },
  { path: 'register', component: Register },
  { path: 'login', component: LoginPlaceholder },
  { path: '**', redirectTo: 'register' },
];
