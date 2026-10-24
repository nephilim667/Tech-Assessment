import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthenticationSession } from '../services/authentication-session';

export const authenticationGuard: CanActivateFn = (_route, state) => {
  const session = inject(AuthenticationSession);
  const router = inject(Router);

  return session.getAccessToken()
    ? true
    : router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url },
      });
};