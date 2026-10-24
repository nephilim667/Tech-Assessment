import { inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { patchState, signalStore, withMethods, withState } from '@ngrx/signals';
import { firstValueFrom, forkJoin, Observable } from 'rxjs';

import { ExpenseReportResponse } from '../../expense-reports/contracts/expense-report-response';
import { ExpenseReportsApi } from '../../expense-reports/services/expense-reports-api';
import { ExpensePageResponse } from '../contracts/expense-page-response';
import { CreateExpenseRequest } from '../contracts/create-expense-request';
import { UpdateExpenseRequest } from '../contracts/update-expense-request';
import { ExpensesMessages } from '../errors/expenses-messages';
import { ExpensesApi } from '../services/expenses-api';

interface ExpensesState {
  report: ExpenseReportResponse | null;
  page: ExpensePageResponse | null;
  isLoading: boolean;
  isSaving: boolean;
  error: string | null;
}

const initialState: ExpensesState = {
  report: null,
  page: null,
  isLoading: false,
  isSaving: false,
  error: null,
};

function getErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0 || error.status === 503) {
    return ExpensesMessages.serviceUnavailable;
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

export const ExpensesStore = signalStore(
  withState(initialState),

  withMethods((store, expensesApi = inject(ExpensesApi), reportsApi = inject(ExpenseReportsApi)) => {
    function isBusy(): boolean {
      return store.isLoading() || store.isSaving();
    }

    async function readPage(reportId: string, pageNumber: number): Promise<ExpensePageResponse> {
      const page = await firstValueFrom(
        expensesApi.getPage(reportId, pageNumber),
      );

      const lastPage = Math.max(1, page.totalPages);

      if (page.pageNumber > lastPage) {
        return firstValueFrom(
          expensesApi.getPage(reportId, lastPage),
        );
      }

      return page;
    }

    async function executeMutation<T>(operation: (reportId: string) => Observable<T>, pageNumber: number, fallbackMessage: string): Promise<boolean> {
      const report = store.report();

      if (!report || isBusy()) {
        return false;
      }

      patchState(store, { isSaving: true, error: null });

      try {
        await firstValueFrom(operation(report.id));
      } 
      catch (error: unknown) {
        patchState(store, { isSaving: false, error: getErrorMessage(error, fallbackMessage) });
        return false;
      }

      // La mutation est confirmée : les anciennes données sont obsolètes.
      patchState(store, { page: null });

      try {
        const page = await readPage(report.id, pageNumber);
        patchState(store, { page });
      } 
      catch {
        patchState(store, { error: ExpensesMessages.refreshFailed });
      } 
      finally {
        patchState(store, { isSaving: false });
      }

      return true;
    }

    return {
      clearError(): void { patchState(store, { error: null }); },
    
      async load(reportId: string): Promise<boolean> {
        if (isBusy()) {
          return false;
        }
      
        patchState(store, { report: null, page: null, isLoading: true, error: null });
      
        try {
          const result = await firstValueFrom(
            forkJoin({
              report: reportsApi.getById(reportId),
              page: expensesApi.getPage(reportId, 1),
            }),
          );
        
          patchState(store, { report: result.report, page: result.page });
          return true;
        } 
        catch (error: unknown) {
          patchState(store, { error: getErrorMessage(error, ExpensesMessages.loadFailed) });
          return false;
        } 
        finally {
          patchState(store, { isLoading: false });
        }
      },
    
      async goToPage(pageNumber: number): Promise<boolean> {
        const report = store.report();
        const currentPage = store.page();
      
        if (isBusy() || !report || !currentPage || !Number.isInteger(pageNumber) || pageNumber < 1 || pageNumber > Math.max(1, currentPage.totalPages)) {
          return false;
        }
      
        patchState(store, { isLoading: true, error: null });
      
        try {
          const page = await readPage(report.id, pageNumber);
        
          patchState(store, { page });
          return true;
        } 
        catch (error: unknown) {
          patchState(store, {
            error: getErrorMessage(error, ExpensesMessages.pageFailed),
          });
        
          return false;
        } 
        finally {
          patchState(store, { isLoading: false });
        }
      },
    
      async refresh(): Promise<boolean> {
        const report = store.report();
      
        if (!report || isBusy()) {
          return false;
        }
      
        const pageNumber = store.page()?.pageNumber ?? 1;
      
        patchState(store, { isLoading: true, error: null });
      
        try {
          const page = await readPage(report.id, pageNumber);
        
          patchState(store, { page });
          return true;
        } 
        catch (error: unknown) {
          patchState(store, {
            error: getErrorMessage(error, ExpensesMessages.pageFailed),
          });
        
          return false;
        } 
        finally {
          patchState(store, { isLoading: false });
        }
      },
    
      create(request: CreateExpenseRequest): Promise<boolean> {
        return executeMutation(
          (reportId) => expensesApi.create(reportId, request),
          1,
          ExpensesMessages.createFailed,
        );
      },
    
      update(expenseId: string, request: UpdateExpenseRequest): Promise<boolean> {
        return executeMutation(
          (reportId) => expensesApi.update(reportId, expenseId, request),
          store.page()?.pageNumber ?? 1,
          ExpensesMessages.updateFailed,
        );
      },
    
      delete(expenseId: string): Promise<boolean> {
        return executeMutation(
          (reportId) => expensesApi.delete(reportId, expenseId),
          store.page()?.pageNumber ?? 1,
          ExpensesMessages.deleteFailed,
        );
      },
    };
  }),
);