import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ExpenseReportResponse } from '../../expense-reports/contracts/expense-report-response';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpenseForm } from './expense-form';

describe('ExpenseForm', () => {
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

  let fixture: ComponentFixture<ExpenseForm>;
  let component: ExpenseForm;

  function fillValidForm(): void {
    component.form.patchValue({
      date: '2026-10-24',
      description: ' Déjeuner professionnel ',
      amount: 25.5,
      billingStreet: ' 10 rue des Lilas ',
      billingPostalCode: ' 75001 ',
      billingCity: ' Paris ',
      billingBrand: ' ',
    });
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ExpenseForm],
    }).compileComponents();

    fixture = TestBed.createComponent(ExpenseForm);
    component = fixture.componentInstance;

    fixture.componentRef.setInput('report', report);

    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('initializes creation for the current report and its month', () => {
    expect(component.form.controls.date.value).toBe('2026-10-01');

    expect(
      component.form.controls.destinationExpenseReportId.value,
    ).toBe(report.id);

    expect(component.form.controls.amount.value).toBeNull();
  });

  it('rejects an incomplete form', () => {
    const emit = vi.spyOn(component.submitted, 'emit');

    component.submit();

    expect(emit).not.toHaveBeenCalled();
    expect(component.form.controls.description.touched).toBe(true);
  });

  it('emits a creation with trimmed fields and no time conversion', () => {
    const emit = vi.spyOn(component.submitted, 'emit');

    fillValidForm();

    component.submit();

    expect(emit).toHaveBeenCalledWith({
      expenseId: null,
      request: {
        date: '2026-10-24',
        description: 'Déjeuner professionnel',
        amount: 25.5,
        billingStreet: '10 rue des Lilas',
        billingPostalCode: '75001',
        billingCity: 'Paris',
        billingBrand: null,
        destinationExpenseReportId: report.id,
      },
    });
  });

  it.each([0, -1, 12.345])(
    'rejects an invalid amount: %s',
    (amount) => {
      const emit = vi.spyOn(component.submitted, 'emit');

      fillValidForm();
      component.form.controls.amount.setValue(amount);

      component.submit();

      expect(component.form.controls.amount.invalid).toBe(true);
      expect(emit).not.toHaveBeenCalled();
    },
  );

  it('initializes editing from the selected expense', async () => {
    fixture.componentRef.setInput('expense', expense);

    fixture.detectChanges();
    await fixture.whenStable();

    expect(component.form.controls.date.value).toBe(expense.date);
    expect(component.form.controls.description.value).toBe(expense.description);
    expect(component.form.controls.amount.value).toBe(expense.amount);
    expect(component.form.pristine).toBe(true);
  });

  it('emits the expense identifier and selected destination when editing', async () => {
    const destination: ExpenseReportResponse = {
      ...report,
      id: 'b6c84430-923b-4ad3-a992-a94eca88914b',
      number: 43,
      reference: 'Note 43',
    };

    fixture.componentRef.setInput('expense', expense);
    fixture.componentRef.setInput('destinations', [destination]);

    fixture.detectChanges();
    await fixture.whenStable();

    const emit = vi.spyOn(component.submitted, 'emit');

    component.form.controls.destinationExpenseReportId
      .setValue(destination.id);

    component.submit();

    expect(emit).toHaveBeenCalledWith({
      expenseId: expense.id,
      request: {
        date: expense.date,
        description: expense.description,
        amount: expense.amount,
        billingStreet: expense.billingStreet,
        billingPostalCode: expense.billingPostalCode,
        billingCity: expense.billingCity,
        billingBrand: null,
        destinationExpenseReportId: destination.id,
      },
    });
  });

  it('prevents submission while saving', () => {
    const emit = vi.spyOn(component.submitted, 'emit');

    fillValidForm();
    fixture.componentRef.setInput('isSaving', true);

    component.submit();

    expect(emit).not.toHaveBeenCalled();
  });

  it('emits cancellation from the editing form', async () => {
    fixture.componentRef.setInput('expense', expense);

    fixture.detectChanges();
    await fixture.whenStable();

    const emit = vi.spyOn(component.cancelled, 'emit');
    const element = fixture.nativeElement as HTMLElement;

    element.querySelector<HTMLButtonElement>(
      'button[type="button"]',
    )!.click();

    expect(emit).toHaveBeenCalledOnce();
  });
});