import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';

import { ExpenseReportResponse } from '../../expense-reports/contracts/expense-report-response';
import { ExpenseReportsApi } from '../../expense-reports/services/expense-reports-api';
import { ExpensePageResponse } from '../contracts/expense-page-response';
import { CreateExpenseRequest } from '../contracts/create-expense-request';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpensesMessages } from '../errors/expenses-messages';
import { ExpensesApi } from '../services/expenses-api';
import { ExpensesStore } from './expenses-store';

describe('ExpensesStore', () => {
  const report: ExpenseReportResponse = {
    id: '05631031-9ff0-4bd5-9c2d-03f2f7879efe',
    userId: '5a5617d5-f041-4a97-bfd0-32b187b10c4b',
    title: 'Juste Leblanc - Octobre 2026',
    year: 2026,
    month: 10,
    number: 42,
    reference: 'Note 42',
  };

  const firstPage: ExpensePageResponse = {
    items: [],
    pageNumber: 1,
    pageSize: 5,
    totalCount: 6,
    totalPages: 2,
    totalAmount: 150,
  };

  const creationRequest: CreateExpenseRequest = {
    date: '2026-10-24',
    description: 'Déjeuner professionnel',
    amount: 25.5,
    billingStreet: '10 rue des Lilas',
    billingPostalCode: '75001',
    billingCity: 'Paris',
    billingBrand: null,
  };

  const expense: ExpenseResponse = {
    ...creationRequest,
    id: '19d4a834-a294-4d6e-9dc2-fbddfa77b32b',
    expenseReportId: report.id,
  };

  let reportsApi: {
    getById: ReturnType<typeof vi.fn>;
  };

  let expensesApi: {
    getPage: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  let store: InstanceType<typeof ExpensesStore>;

  beforeEach(() => {
    reportsApi = {
      getById: vi.fn().mockReturnValue(of(report)),
    };

    expensesApi = {
      getPage: vi.fn().mockReturnValue(of(firstPage)),
      create: vi.fn(),
      update: vi.fn(),
      delete: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        ExpensesStore,
        { provide: ExpenseReportsApi, useValue: reportsApi },
        { provide: ExpensesApi, useValue: expensesApi },
      ],
    });

    store = TestBed.inject(ExpensesStore);
  });

  it('loads the report and first page with the API totals', async () => {
    const response = new Subject<ExpensePageResponse>();

    expensesApi.getPage.mockReturnValue(response);

    const loading = store.load(report.id);

    expect(store.isLoading()).toBe(true);

    response.next(firstPage);
    response.complete();

    expect(await loading).toBe(true);
    expect(reportsApi.getById).toHaveBeenCalledWith(report.id);
    expect(expensesApi.getPage).toHaveBeenCalledWith(report.id, 1);
    expect(store.report()).toEqual(report);
    expect(store.page()).toEqual(firstPage);
    expect(store.page()?.totalAmount).toBe(150);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('clears the previous report when another report fails to load', async () => {
    await store.load(report.id);

    reportsApi.getById.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    expect(await store.load('another-report')).toBe(false);

    expect(store.report()).toBeNull();
    expect(store.page()).toBeNull();
    expect(store.error()).toBe(ExpensesMessages.serviceUnavailable);
    expect(store.isLoading()).toBe(false);
  });

  it('loads the selected page without reloading the report', async () => {
    await store.load(report.id);

    const secondPage: ExpensePageResponse = {
      ...firstPage,
      pageNumber: 2,
    };

    expensesApi.getPage.mockReturnValue(of(secondPage));

    expect(await store.goToPage(2)).toBe(true);

    expect(expensesApi.getPage).toHaveBeenLastCalledWith(report.id, 2);
    expect(reportsApi.getById).toHaveBeenCalledOnce();
    expect(store.page()).toEqual(secondPage);
  });

  it.each([0, -1, 1.5, 3])(
    'does not request an invalid or unavailable page: %s',
    async (pageNumber) => {
      await store.load(report.id);

      expensesApi.getPage.mockClear();

      expect(await store.goToPage(pageNumber)).toBe(false);
      expect(expensesApi.getPage).not.toHaveBeenCalled();
      expect(store.page()).toEqual(firstPage);
    },
  );

  it('preserves the displayed page when navigation fails', async () => {
    await store.load(report.id);

    expensesApi.getPage.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    expect(await store.goToPage(2)).toBe(false);

    expect(store.page()).toEqual(firstPage);
    expect(store.error()).toBe(ExpensesMessages.pageFailed);
    expect(store.isLoading()).toBe(false);

    store.clearError();

    expect(store.error()).toBeNull();
  });

  it('prevents another load while a request is pending', async () => {
    const response = new Subject<ExpensePageResponse>();

    expensesApi.getPage.mockReturnValue(response);

    const loading = store.load(report.id);

    expect(await store.load('another-report')).toBe(false);
    expect(await store.goToPage(2)).toBe(false);

    expect(reportsApi.getById).toHaveBeenCalledOnce();
    expect(expensesApi.getPage).toHaveBeenCalledOnce();

    response.next(firstPage);
    response.complete();

    await loading;
  });

  it('creates an expense and reloads the first page and totals', async () => {
    await store.load(report.id);

    const refreshed: ExpensePageResponse = {
      ...firstPage,
      items: [expense],
      totalAmount: 175.5,
      totalCount: 7,
    };

    expensesApi.create.mockReturnValue(of(expense));
    expensesApi.getPage.mockReturnValue(of(refreshed));

    expect(await store.create(creationRequest)).toBe(true);

    expect(expensesApi.create).toHaveBeenCalledWith(
      report.id,
      creationRequest,
    );

    expect(expensesApi.getPage).toHaveBeenLastCalledWith(report.id, 1);
    expect(store.page()).toEqual(refreshed);
    expect(store.isSaving()).toBe(false);
  });

  it('updates an expense and reloads the current source page', async () => {
    await store.load(report.id);

    expensesApi.getPage.mockReturnValue(of({
      ...firstPage,
      pageNumber: 2,
    }));

    await store.goToPage(2);

    const update = {
      ...creationRequest,
      destinationExpenseReportId: 'b6c84430-923b-4ad3-a992-a94eca88914b',
    };

    expensesApi.update.mockReturnValue(of({
      ...expense,
      expenseReportId: update.destinationExpenseReportId,
    }));

    expect(await store.update(expense.id, update)).toBe(true);

    expect(expensesApi.update).toHaveBeenCalledWith(
      report.id,
      expense.id,
      update,
    );

    expect(expensesApi.getPage).toHaveBeenLastCalledWith(report.id, 2);
  });

  it('returns to the last available page after deleting its final item', async () => {
    await store.load(report.id);

    expensesApi.getPage.mockReturnValueOnce(of({
      ...firstPage,
      pageNumber: 2,
      items: [expense],
    }));

    await store.goToPage(2);

    expensesApi.delete.mockReturnValue(of(undefined));

    const remainingPage: ExpensePageResponse = {
      ...firstPage,
      pageNumber: 1,
      totalCount: 5,
      totalPages: 1,
      totalAmount: 124.5,
    };

    expensesApi.getPage
      .mockReturnValueOnce(of({
        ...remainingPage,
        pageNumber: 2,
        items: [],
      }))
      .mockReturnValueOnce(of(remainingPage));

    expect(await store.delete(expense.id)).toBe(true);

    expect(expensesApi.delete).toHaveBeenCalledWith(report.id, expense.id);
    expect(expensesApi.getPage).toHaveBeenLastCalledWith(report.id, 1);
    expect(store.page()).toEqual(remainingPage);
  });

  it('preserves the displayed page when a mutation is refused', async () => {
    await store.load(report.id);

    expensesApi.getPage.mockClear();

    expensesApi.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: { detail: 'Le quota mensuel est atteint.' },
      })),
    );

    expect(await store.create(creationRequest)).toBe(false);

    expect(store.page()).toEqual(firstPage);
    expect(store.error()).toBe('Le quota mensuel est atteint.');
    expect(expensesApi.getPage).not.toHaveBeenCalled();
    expect(store.isSaving()).toBe(false);
  });

  it('reports mutation success even if refreshing the list fails', async () => {
    await store.load(report.id);

    expensesApi.create.mockReturnValue(of(expense));

    expensesApi.getPage.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    expect(await store.create(creationRequest)).toBe(true);

    expect(store.page()).toBeNull();
    expect(store.report()).toEqual(report);
    expect(store.error()).toBe(ExpensesMessages.refreshFailed);
    expect(expensesApi.create).toHaveBeenCalledOnce();
    expect(store.isSaving()).toBe(false);
  });

  it('blocks other operations while a mutation is pending', async () => {
    await store.load(report.id);

    const response = new Subject<ExpenseResponse>();

    expensesApi.create.mockReturnValue(response);

    const creation = store.create(creationRequest);

    expect(store.isSaving()).toBe(true);
    expect(await store.create(creationRequest)).toBe(false);
    expect(await store.delete(expense.id)).toBe(false);
    expect(await store.load('another-report')).toBe(false);
    expect(await store.goToPage(2)).toBe(false);

    expect(expensesApi.create).toHaveBeenCalledOnce();
    expect(expensesApi.delete).not.toHaveBeenCalled();

    response.next(expense);
    response.complete();

    await creation;

    expect(store.isSaving()).toBe(false);
  });

  it('does not mutate expenses before a report is loaded', async () => {
    expect(await store.create(creationRequest)).toBe(false);
    expect(await store.delete(expense.id)).toBe(false);

    expect(expensesApi.create).not.toHaveBeenCalled();
    expect(expensesApi.delete).not.toHaveBeenCalled();
  });
});