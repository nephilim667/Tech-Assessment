import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom, Observable } from 'rxjs';

import { CreateUserRequest } from '../contracts/create-user-request';
import { UserResponse } from '../contracts/user-response';
import { UsersMessages } from '../errors/users-messages';
import { UsersApi } from '../services/users-api';

interface UsersState {
  users: UserResponse[];
  isLoading: boolean;
  isSaving: boolean;
  error: string | null;
}

const initialState: UsersState = {
  users: [],
  isLoading: false,
  isSaving: false,
  error: null,
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0 || error.status === 503) {
    return UsersMessages.serviceUnavailable;
  }

  const body: unknown = error.error;

  if (error.status >= 400 && error.status < 500 &&
      typeof body === 'object' && body !== null &&
      'title' in body && typeof body.title === 'string' && 
    body.title.trim().length > 0) {
    return body.title;
  }

  return fallback;
}

export const UsersStore = signalStore(
  withState(initialState),

  withMethods((store, api = inject(UsersApi)) => {
    async function executeMutation<T>(operation: () => Observable<T>, onSuccess: (response: T) => void, fallbackMessage: string): Promise<boolean> {
      if (store.isSaving()) {
        return false;
      }

      patchState(store, {
        isSaving: true,
        error: null,
      });

      try {
        const response = await firstValueFrom(operation());

        onSuccess(response);

        return true;
      } 
      catch (error: unknown) {
        patchState(store, {
          error: getErrorMessage(error, fallbackMessage),
        });

        return false;
      } 
      finally {
        patchState(store, { isSaving: false });
      }
    }

    return {
      clearError(): void {
        patchState(store, { error: null });
      },

      async load(): Promise<void> {
        if (store.isLoading()) {
          return;
        }

        patchState(store, {
          isLoading: true,
          error: null,
        });

        try {
          const users = await firstValueFrom(api.getAll());

          patchState(store, { users });
        } 
        catch (error: unknown) {
          patchState(store, {
            error: getErrorMessage(error, UsersMessages.loadFailed),
          });
        } 
        finally {
          patchState(store, { isLoading: false });
        }
      },

      create(request: CreateUserRequest): Promise<boolean> {
        return executeMutation(
          () => api.create(request),
          (user) => {
            patchState(store, {
              users: [...store.users(), user],
            });
          },
          UsersMessages.createFailed,
        );
      },

      setStatus(id: string, isActive: boolean): Promise<boolean> {
        return executeMutation(
          () => api.setStatus(id, isActive),
          (updatedUser) => {
            patchState(store, {
              users: store.users().map((user) =>
                user.id === updatedUser.id ? updatedUser : user,
              ),
            });
          },
          UsersMessages.statusFailed,
        );
      },

      delete(id: string): Promise<boolean> {
        return executeMutation(
          () => api.delete(id),
          () => {
            patchState(store, {
              users: store.users().filter((user) => user.id !== id),
            });
          },
          UsersMessages.deleteFailed,
        );
      },
    };
  }),
);