import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthenticationSession } from '../services/authentication-session';

export const superAdministratorGuard: CanActivateFn = () => {
  const session = inject(AuthenticationSession);
  const router = inject(Router);

  if (!session.getAccessToken()) {
    return router.parseUrl('/login');
  }

  return session.isSuperAdmin()
    ? true
    : router.parseUrl('/users');
};