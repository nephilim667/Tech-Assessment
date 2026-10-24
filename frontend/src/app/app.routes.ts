import { Routes } from '@angular/router';

import { authenticationGuard } from './core/authentication/guards/authentication-guard';
import { guestGuard } from './core/authentication/guards/guest-guard';

export const appRoutes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () =>
      import('./features/authentication/pages/login-page')
        .then(module => module.LoginPage),
  },
  {
    path: 'users',
    canActivate: [authenticationGuard],
    loadComponent: () =>
      import('./features/users/pages/users-page')
        .then(module => module.UsersPage),
  },
  { path: '', pathMatch: 'full', redirectTo: 'users' },
  { path: '**', redirectTo: 'users' },
];