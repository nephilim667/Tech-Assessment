import { InjectionToken } from '@angular/core';

export const AUTH_SESSION_STORAGE = new InjectionToken<Storage>(
  'AUTH_SESSION_STORAGE',
  {
    providedIn: 'root',
    factory: () => sessionStorage,
  },
);