import { computed, inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { AuthenticationRequest } from '../contracts/authentication-request';
import { AuthenticationApi } from '../services/authentication-api';
import { AuthenticationSession } from '../services/authentication-session';
import { AuthenticationMessages } from '../errors/authentication-messages';

interface AuthenticationState {
  isPending: boolean;
  error: string | null;
}

const initialState: AuthenticationState = {
  isPending: false,
  error: null,
};

export const AuthenticationStore = signalStore(
  { providedIn: 'root' },

  withState(initialState),

  withComputed((_store, session = inject(AuthenticationSession)) => ({
    hasSession: computed(() => session.session() !== null),
  })),

  withMethods((store, api = inject(AuthenticationApi), session = inject(AuthenticationSession)) => ({
    clearError(): void { patchState(store, { error: null }); },

    async authenticate(request: AuthenticationRequest): Promise<boolean> {
      if (store.isPending()) {
        return false;
      }

      patchState(store, { isPending: true, error: null });

      try {
        const response = await firstValueFrom(
          api.authenticate(request),
        );

        session.start(response);
        return true;
      } 
      catch (error: unknown) {
        let message: string = AuthenticationMessages.loginFailed;

        if (error instanceof HttpErrorResponse) {
          if (error.status === 401) {
            message = AuthenticationMessages.invalidCredentials;
          } else if (error.status === 0 || error.status === 503) {
            message = AuthenticationMessages.serviceUnavailable;
          }
        }

        patchState(store, { error: message });
        return false;
      } 
      finally {
        patchState(store, { isPending: false });
      }
    },

    async logout(): Promise<boolean> {
      if (store.isPending()) {
        return false;
      }

      patchState(store, { isPending: true, error: null });

      try {
        if (!session.getAccessToken()) {
          session.clear();
          return true;
        }

        await firstValueFrom(api.logout());

        session.clear();
        return true;
      } 
      catch (error: unknown) {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          // Le serveur n’accepte déjà plus ce token.
          session.clear();
          return true;
        }

        const unavailable = error instanceof HttpErrorResponse && (error.status === 0 || error.status === 503);
        patchState(store, {
          error: unavailable ? AuthenticationMessages.serviceUnavailable : AuthenticationMessages.logoutFailed,
        });

        return false;
      } 
      finally {
        patchState(store, { isPending: false });
      }
    },
  })),
);