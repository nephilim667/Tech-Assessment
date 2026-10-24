import { inject } from '@angular/core';
import {
  HttpErrorResponse,
  HttpInterceptorFn,
} from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { AuthenticationSession } from '../services/authentication-session';

export const authenticationInterceptor: HttpInterceptorFn = (request, next,) => {
  const path = request.url.split('?')[0];

  if (!request.url.startsWith('/api/') || path === '/api/auth/login') {
    return next(request);
  }

  const session = inject(AuthenticationSession);
  const token = session.getAccessToken();

  const authenticatedRequest = token
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`,
        },
      })
    : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401 && token && session.session()?.accessToken === token) {
        session.clear();
      }

      return throwError(() => error);
    }),
  );
};