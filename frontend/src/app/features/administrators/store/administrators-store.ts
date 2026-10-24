import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom } from 'rxjs';

import { CreateAdministratorRequest } from '../contracts/administrator-account';
import { AdministratorsMessages } from '../errors/administrators-messages';
import { AdministratorsApi } from '../services/administrators-api';

interface AdministratorsState {
  isSaving: boolean;
  error: string | null;
  createdEmail: string | null;
}

const initialState: AdministratorsState = {
  isSaving: false,
  error: null,
  createdEmail: null,
};

function getErrorMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return AdministratorsMessages.createFailed;
  }

  if (error.status === 0 || error.status === 503) {
    return AdministratorsMessages.serviceUnavailable;
  }

  if (error.status === 403) {
    return AdministratorsMessages.forbidden;
  }

  const body: unknown = error.error;

  if (error.status >= 400 && error.status < 500 && typeof body === 'object' &&
      body !== null && 'title' in body && typeof body.title === 'string' && body.title.trim().length > 0) {
    return body.title;
  }

  return AdministratorsMessages.createFailed;
}

export const AdministratorsStore = signalStore(
  withState(initialState),

  withMethods((store, api = inject(AdministratorsApi)) => ({
    async create(request: CreateAdministratorRequest): Promise<boolean> {
      if (store.isSaving()) {
        return false;
      }

      patchState(store, {
        isSaving: true,
        error: null,
        createdEmail: null,
      });

      try {
        const account = await firstValueFrom(api.create(request));

        patchState(store, {
          createdEmail: account.email,
        });

        return true;
      } catch (error: unknown) {
        patchState(store, {
          error: getErrorMessage(error),
        });

        return false;
      } finally {
        patchState(store, { isSaving: false });
      }
    },
  })),
);