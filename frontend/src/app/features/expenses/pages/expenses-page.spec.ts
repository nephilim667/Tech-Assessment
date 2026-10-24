import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';

import { ExpenseReportResponse } from '../../expense-reports/contracts/expense-report-response';
import { ExpenseReportPeriod } from '../../expense-reports/contracts/expense-report-period';
import { ExpenseReportsStore } from '../../expense-reports/store/expense-reports-store';
import { ExpensePageResponse } from '../contracts/expense-page-response';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpenseSubmission } from '../contracts/expense-submission';
import { ExpensesStore } from '../store/expenses-store';
import { ExpensesPage } from './expenses-page';

describe('ExpensesPage', () => {
  const report: ExpenseReportResponse = {
    id: '05631031-9ff0-4bd5-9c2d-03f2f7879efe',
    userId: '5a5617d5-f041-4a97-bfd0-32b187b10c4b',
    title: 'Juste Leblanc - Octobre 2026',
    year: 2026,
    month: 10,
    number: 42,
    reference: 'Note 42',
  };

  const expense: ExpenseResponse = {
    id: '19d4a834-a294-4d6e-9dc2-fbddfa77b32b',
    expenseReportId: report.id,
    date: '2026-10-24',
    description: 'Déjeuner professionnel',
    amount: 25.5,
    billingStreet: '10 rue des Lilas',
    billingPostalCode: '75001',
    billingCity: 'Paris',
    billingBrand: null,
  };

  const submission: ExpenseSubmission = {
    expenseId: null,
    request: {
      date: expense.date,
      description: expense.description,
      amount: expense.amount,
      billingStreet: expense.billingStreet,
      billingPostalCode: expense.billingPostalCode,
      billingCity: expense.billingCity,
      billingBrand: expense.billingBrand,
      destinationExpenseReportId: report.id,
    },
  };

  const pageData: ExpensePageResponse = {
    items: [expense],
    pageNumber: 1,
    pageSize: 5,
    totalCount: 1,
    totalPages: 1,
    totalAmount: 25.5,
  };

  let fixture: ComponentFixture<ExpensesPage>;
  let page: ExpensesPage;

  let store: {
    report: ReturnType<typeof signal<ExpenseReportResponse | null>>;
    page: ReturnType<typeof signal<ExpensePageResponse | null>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    load: ReturnType<typeof vi.fn>;
    refresh: ReturnType<typeof vi.fn>;
    goToPage: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    update: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  let destinationsStore: {
    reports: ReturnType<typeof signal<ExpenseReportResponse[]>>;
    period: ReturnType<typeof signal<ExpenseReportPeriod | null>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    load: ReturnType<typeof vi.fn>;
    resetSelection: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    store = {
      report: signal<ExpenseReportResponse | null>(report),
      page: signal<ExpensePageResponse | null>(pageData),
      isLoading: signal(false),
      isSaving: signal(false),
      error: signal<string | null>(null),
      load: vi.fn().mockResolvedValue(true),
      refresh: vi.fn().mockResolvedValue(true),
      goToPage: vi.fn().mockResolvedValue(true),
      create: vi.fn().mockResolvedValue(true),
      update: vi.fn().mockResolvedValue(true),
      delete: vi.fn().mockResolvedValue(true),
    };

    destinationsStore = {
      reports: signal<ExpenseReportResponse[]>([]),
      period: signal<ExpenseReportPeriod | null>(null),
      isLoading: signal(false),
      error: signal<string | null>(null),
      load: vi.fn().mockResolvedValue(true),
      resetSelection: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ExpensesPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap({
                expenseReportId: report.id,
              }),
            },
          },
        },
      ],
    })
      .overrideComponent(ExpensesPage, {
        set: {
          providers: [
            { provide: ExpensesStore, useValue: store },
            { provide: ExpenseReportsStore, useValue: destinationsStore },
          ],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ExpensesPage);
    page = fixture.componentInstance;

    fixture.detectChanges();
    await fixture.whenStable();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads the report identified by the route and displays its expenses', () => {
    expect(store.load).toHaveBeenCalledWith(report.id);

    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Note 42');
    expect(element.textContent).toContain('Déjeuner professionnel');
  });

  it('creates an expense without sending the destination property', async () => {
    await page.save(submission);

    expect(store.create).toHaveBeenCalledWith({
      date: expense.date,
      description: expense.description,
      amount: expense.amount,
      billingStreet: expense.billingStreet,
      billingPostalCode: expense.billingPostalCode,
      billingCity: expense.billingCity,
      billingBrand: expense.billingBrand,
    });
  });

  it('searches destinations for the same beneficiary when editing', async () => {
    await page.editExpense(expense);

    expect(page.editing()).toEqual(expense);
    expect(destinationsStore.load).toHaveBeenCalledWith({
      userId: report.userId,
      year: report.year,
      month: report.month,
    });
  });

  it('updates the expense and leaves editing after success', async () => {
    page.editing.set(expense);

    await page.save({
      ...submission,
      expenseId: expense.id,
    });

    expect(store.update).toHaveBeenCalledWith(
      expense.id,
      submission.request,
    );

    expect(page.editing()).toBeNull();
  });

  it('keeps editing when the API refuses the update', async () => {
    store.update.mockResolvedValue(false);
    page.editing.set(expense);

    await page.save({
      ...submission,
      expenseId: expense.id,
    });

    expect(page.editing()).toEqual(expense);
  });

  it('does not delete an expense when confirmation is cancelled', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    await page.deleteExpense(expense);

    expect(store.delete).not.toHaveBeenCalled();
  });

  it('deletes the expense after confirmation and closes its editor', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    page.editing.set(expense);

    await page.deleteExpense(expense);

    expect(store.delete).toHaveBeenCalledWith(expense.id);
    expect(page.editing()).toBeNull();
  });

  it('prevents saving while another operation is pending', async () => {
    store.isSaving.set(true);

    await page.save(submission);

    expect(store.create).not.toHaveBeenCalled();
    expect(store.update).not.toHaveBeenCalled();
  });
});