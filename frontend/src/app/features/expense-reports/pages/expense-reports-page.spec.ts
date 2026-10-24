import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserResponse } from '../../users/contracts/user-response';
import { UsersStore } from '../../users/store/users-store';
import { ExpenseReportPeriod } from '../contracts/expense-report-period';
import { ExpenseReportResponse } from '../contracts/expense-report-response';
import { ExpenseReportsStore } from '../store/expense-reports-store';
import { ExpenseReportsPage } from './expense-reports-page';

describe('ExpenseReportsPage', () => {
  const user: UserResponse = {
    id: '5a5617d5-f041-4a97-bfd0-32b187b10c4b',
    firstName: 'Juste',
    lastName: 'Leblanc',
    street: '10 rue des Lilas',
    postalCode: '75001',
    city: 'Paris',
    maxExpensesPerMonth: 5,
    isActive: true,
  };

  const period: ExpenseReportPeriod = {
    userId: user.id,
    year: 2026,
    month: 10,
  };

  const report: ExpenseReportResponse = {
    id: '05631031-9ff0-4bd5-9c2d-03f2f7879efe',
    userId: user.id,
    title: 'Juste Leblanc - Octobre 2026',
    year: 2026,
    month: 10,
    reference: '05631031-9ff0-4bd5-9c2d-03f2f7879efe',
  };

  let fixture: ComponentFixture<ExpenseReportsPage>;
  let page: ExpenseReportsPage;

  let usersStore: {
    users: ReturnType<typeof signal<UserResponse[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    load: ReturnType<typeof vi.fn>;
  };

  let store: {
    period: ReturnType<typeof signal<ExpenseReportPeriod | null>>;
    reports: ReturnType<typeof signal<ExpenseReportResponse[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    load: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
    resetSelection: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    usersStore = {
      users: signal([user]),
      isLoading: signal(false),
      error: signal<string | null>(null),
      load: vi.fn().mockResolvedValue(undefined),
    };

    store = {
      period: signal<ExpenseReportPeriod | null>(null),
      reports: signal<ExpenseReportResponse[]>([]),
      isLoading: signal(false),
      isSaving: signal(false),
      error: signal<string | null>(null),
      load: vi.fn().mockResolvedValue(true),
      create: vi.fn().mockResolvedValue(true),
      delete: vi.fn().mockResolvedValue(true),
      resetSelection: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [ExpenseReportsPage],
    })
      .overrideComponent(ExpenseReportsPage, {
        set: {
          providers: [
            { provide: UsersStore, useValue: usersStore },
            { provide: ExpenseReportsStore, useValue: store },
          ],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(ExpenseReportsPage);
    page = fixture.componentInstance;

    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads the users when the page opens', () => {
    expect(usersStore.load).toHaveBeenCalledOnce();
  });

  it('clears the displayed selection when the criteria are incomplete', async () => {
    await page.search();

    expect(store.load).not.toHaveBeenCalled();
    expect(store.resetSelection).toHaveBeenCalledOnce();
  });

  it('automatically loads reports when the beneficiary changes', async () => {
    const element = fixture.nativeElement as HTMLElement;
    const select = element.querySelector<HTMLSelectElement>('#report-user')!;

    select.value = user.id;
    select.dispatchEvent(new Event('change', { bubbles: true }));

    await fixture.whenStable();

    expect(store.load).toHaveBeenCalledWith({
      userId: user.id,
      year: page.form.controls.year.value,
      month: page.form.controls.month.value,
    });
  });

  it('searches using the selected user and period', async () => {
    page.form.setValue(period);

    await page.search();

    expect(store.load).toHaveBeenCalledWith(period);
  });

  it('creates a report for the displayed selection', async () => {
    page.form.setValue(period);
    store.period.set(period);

    await page.createReport();

    expect(store.create).toHaveBeenCalledOnce();
  });

  it('prevents creation when the selection differs from the displayed period', async () => {
    store.period.set(period);
    page.form.setValue({ ...period, month: 11 });

    await page.createReport();

    expect(store.create).not.toHaveBeenCalled();
  });

  it('allows consultation but prevents creation for an inactive user', async () => {
    usersStore.users.set([{ ...user, isActive: false }]);
    page.form.setValue(period);

    await page.search();

    expect(store.load).toHaveBeenCalledWith(period);

    store.period.set(period);

    await page.createReport();

    expect(store.create).not.toHaveBeenCalled();
  });

  it('does not delete a report when confirmation is cancelled', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    await page.deleteReport(report);

    expect(store.delete).not.toHaveBeenCalled();
  });

  it('deletes the selected report after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    await page.deleteReport(report);

    expect(store.delete).toHaveBeenCalledWith(report.id);
  });
});