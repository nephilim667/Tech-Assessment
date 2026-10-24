import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthenticationSession } from '../services/authentication-session';

export const guestGuard: CanActivateFn = () => {
  const session = inject(AuthenticationSession);
  const router = inject(Router);

  return session.getAccessToken()
    ? router.createUrlTree(['/users'])
    : true;
};