import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';

import { ExpenseReportPeriod } from '../contracts/expense-report-period';
import { ExpenseReportResponse } from '../contracts/expense-report-response';
import { ExpenseReportsMessages } from '../errors/expense-reports-messages';
import { ExpenseReportsApi } from '../services/expense-reports-api';
import { ExpenseReportsStore } from './expense-reports-store';

describe('ExpenseReportsStore', () => {
  const period: ExpenseReportPeriod = {
    userId: '5a5617d5-f041-4a97-bfd0-32b187b10c4b',
    year: 2026,
    month: 10,
  };

  const report: ExpenseReportResponse = {
    id: '05631031-9ff0-4bd5-9c2d-03f2f7879efe',
    userId: period.userId,
    title: 'Juste Leblanc - Octobre 2026',
    year: period.year,
    month: period.month,
    reference: '05631031-9ff0-4bd5-9c2d-03f2f7879efe',
  };

  let api: {
    getByPeriod: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  let store: InstanceType<typeof ExpenseReportsStore>;

  beforeEach(() => {
    api = {
      getByPeriod: vi.fn(),
      create: vi.fn(),
      delete: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        ExpenseReportsStore,
        { provide: ExpenseReportsApi, useValue: api },
      ],
    });

    store = TestBed.inject(ExpenseReportsStore);
  });

  it('loads the reports and records the displayed period', async () => {
    const response = new Subject<ExpenseReportResponse[]>();

    api.getByPeriod.mockReturnValue(response);

    const loading = store.load(period);

    expect(store.isLoading()).toBe(true);
    expect(store.period()).toBeNull();

    response.next([report]);
    response.complete();

    expect(await loading).toBe(true);
    expect(api.getByPeriod).toHaveBeenCalledWith(period);
    expect(store.reports()).toEqual([report]);
    expect(store.period()).toEqual(period);
    expect(store.isLoading()).toBe(false);
  });

  it('accepts a successfully loaded period with no reports', async () => {
    api.getByPeriod.mockReturnValue(of([]));

    expect(await store.load(period)).toBe(true);
    expect(store.period()).toEqual(period);
    expect(store.reports()).toEqual([]);
    expect(store.error()).toBeNull();
  });

  it('clears the previous results when another period fails to load', async () => {
    api.getByPeriod.mockReturnValueOnce(of([report]));

    await store.load(period);

    api.getByPeriod.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    const success = await store.load({
      ...period,
      month: 11,
    });

    expect(success).toBe(false);
    expect(store.reports()).toEqual([]);
    expect(store.period()).toBeNull();
    expect(store.error()).toBe(
      ExpenseReportsMessages.serviceUnavailable,
    );
    expect(store.isLoading()).toBe(false);
  });

  it('does not create a report before a period has been loaded', async () => {
    expect(await store.create()).toBe(false);
    expect(api.create).not.toHaveBeenCalled();
  });

  it('creates a report using the displayed period', async () => {
    api.getByPeriod.mockReturnValue(of([]));

    await store.load(period);

    api.create.mockReturnValue(of(report));

    expect(await store.create()).toBe(true);
    expect(api.create).toHaveBeenCalledWith(period);
    expect(store.reports()).toEqual([report]);
    expect(store.isSaving()).toBe(false);
  });

  it('removes a report only after the API confirms deletion', async () => {
    api.getByPeriod.mockReturnValue(of([report]));

    await store.load(period);

    const response = new Subject<void>();

    api.delete.mockReturnValue(response);

    const deletion = store.delete(report.id);

    expect(store.reports()).toEqual([report]);
    expect(store.isSaving()).toBe(true);

    response.next();
    response.complete();

    expect(await deletion).toBe(true);
    expect(store.reports()).toEqual([]);
    expect(store.isSaving()).toBe(false);
  });

  it('preserves the report and displays the API detail when deletion fails', async () => {
    api.getByPeriod.mockReturnValue(of([report]));

    await store.load(period);

    api.delete.mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: {
          title: 'Conflict',
          detail: 'La suppression est interdite.',
        },
      })),
    );

    expect(await store.delete(report.id)).toBe(false);
    expect(store.reports()).toEqual([report]);
    expect(store.error()).toBe('La suppression est interdite.');
    expect(store.isSaving()).toBe(false);
  });

  it('prevents another mutation or period change during a save', async () => {
    api.getByPeriod.mockReturnValue(of([]));

    await store.load(period);

    const response = new Subject<ExpenseReportResponse>();

    api.create.mockReturnValue(response);

    const creation = store.create();

    expect(await store.create()).toBe(false);
    expect(await store.load({ ...period, month: 11 })).toBe(false);
    expect(api.create).toHaveBeenCalledOnce();
    expect(api.getByPeriod).toHaveBeenCalledOnce();

    response.next(report);
    response.complete();

    await creation;
  });

  it('exposes a fallback error and allows it to be cleared', async () => {
    api.getByPeriod.mockReturnValue(of([]));

    await store.load(period);

    api.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    expect(await store.create()).toBe(false);
    expect(store.error()).toBe(ExpenseReportsMessages.createFailed);
    expect(store.reports()).toEqual([]);

    store.clearError();

    expect(store.error()).toBeNull();
  });

  it('clears the reports and period when the selection is reset', async () => {
    api.getByPeriod.mockReturnValue(of([report]));

    await store.load(period);

    store.resetSelection();

    expect(store.period()).toBeNull();
    expect(store.reports()).toEqual([]);
    expect(store.error()).toBeNull();
  });
});