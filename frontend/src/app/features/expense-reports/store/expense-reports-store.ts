import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom, Observable } from 'rxjs';

import { ExpenseReportPeriod } from '../contracts/expense-report-period';
import { ExpenseReportResponse } from '../contracts/expense-report-response';
import { ExpenseReportsMessages } from '../errors/expense-reports-messages';
import { ExpenseReportsApi } from '../services/expense-reports-api';

interface ExpenseReportsState {
  period: ExpenseReportPeriod | null;
  reports: ExpenseReportResponse[];
  isLoading: boolean;
  isSaving: boolean;
  error: string | null;
}

const initialState: ExpenseReportsState = {
  period: null,
  reports: [],
  isLoading: false,
  isSaving: false,
  error: null,
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0 || error.status === 503) {
    return ExpenseReportsMessages.serviceUnavailable;
  }

  const body: unknown = error.error;

  if (error.status < 400 || error.status >= 500 || typeof body !== 'object' || body === null) {
    return fallback;
  }

  if ('detail' in body && typeof body.detail === 'string' && body.detail.trim().length > 0) {
    return body.detail;
  }

  if ('title' in body && typeof body.title === 'string' && body.title.trim().length > 0) {
    return body.title;
  }

  return fallback;
}

export const ExpenseReportsStore = signalStore(
  withState(initialState),

  withMethods((store, api = inject(ExpenseReportsApi)) => {
    function isBusy(): boolean {
      return store.isLoading() || store.isSaving();
    }

    async function executeMutation<T>(operation: () => Observable<T>, onSuccess: (response: T) => void, fallbackMessage: string): Promise<boolean> {
      if (isBusy()) {
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
      } catch (error: unknown) {
        patchState(store, {
          error: getErrorMessage(error, fallbackMessage),
        });

        return false;
      } finally {
        patchState(store, { isSaving: false });
      }
    }

    return {
      clearError(): void {
        patchState(store, { error: null });
      },

      async load(period: ExpenseReportPeriod): Promise<boolean> {
        if (isBusy()) {
          return false;
        }

        const selectedPeriod = { ...period };

        patchState(store, {
          period: null,
          reports: [],
          isLoading: true,
          error: null,
        });

        try {
          const reports = await firstValueFrom(
            api.getByPeriod(selectedPeriod),
          );

          patchState(store, {
            period: selectedPeriod,
            reports,
          });

          return true;
        } catch (error: unknown) {
          patchState(store, {
            error: getErrorMessage(
              error,
              ExpenseReportsMessages.loadFailed,
            ),
          });

          return false;
        } finally {
          patchState(store, { isLoading: false });
        }
      },

      resetSelection(): void {
        if (isBusy()) {
          return;
        }

        patchState(store, {
          period: null,
          reports: [],
          error: null,
        });
      },

      create(): Promise<boolean> {
        const period = store.period();

        if (!period) {
          return Promise.resolve(false);
        }

        return executeMutation(
          () => api.create({ ...period }),
          (report) => {
            patchState(store, {
              reports: [...store.reports(), report],
            });
          },
          ExpenseReportsMessages.createFailed,
        );
      },

      delete(id: string): Promise<boolean> {
        return executeMutation(
          () => api.delete(id),
          () => {
            patchState(store, {
              reports: store.reports().filter(
                (report) => report.id !== id,
              ),
            });
          },
          ExpenseReportsMessages.deleteFailed,
        );
      },
    };
  }),
);