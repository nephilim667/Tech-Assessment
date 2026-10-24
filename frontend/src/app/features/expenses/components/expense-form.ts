import { Component, computed, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { ExpenseReportResponse } from '../../expense-reports/contracts/expense-report-response';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpenseSubmission } from '../contracts/expense-submission';

@Component({
  selector: 'app-expense-form',
  imports: [ReactiveFormsModule],
  templateUrl: './expense-form.html',
})
export class ExpenseForm {
  private readonly builder = inject(NonNullableFormBuilder);

  readonly report = input.required<ExpenseReportResponse>();
  readonly expense = input<ExpenseResponse | null>(null);
  readonly destinations = input<ExpenseReportResponse[]>([]);
  readonly isSaving = input(false);

  readonly submitted = output<ExpenseSubmission>();
  readonly cancelled = output<void>();

  readonly billingFields = [
    { name: 'billingStreet', label: 'Adresse de facturation' },
    { name: 'billingPostalCode', label: 'Code postal de facturation' },
    { name: 'billingCity', label: 'Ville de facturation' },
  ] as const;

  readonly availableDestinations = computed(() => [
    this.report(),
    ...this.destinations().filter(
      (destination) => destination.id !== this.report().id,
    ),
  ]);

  readonly form = this.builder.group({
    date: ['', Validators.required],
    description: [
      '',
      [
        Validators.required,
        Validators.pattern(/\S/),
        Validators.maxLength(50),
      ],
    ],
    amount: this.builder.control<number | null>(
      null,
      [
        Validators.required,
        Validators.min(0.01),
        Validators.pattern(/^\d+(\.\d{1,2})?$/),
      ],
    ),
    billingStreet: [ '', [Validators.required, Validators.pattern(/\S/)]],
    billingPostalCode: [ '', [Validators.required, Validators.pattern(/\S/)]],
    billingCity: [ '', [Validators.required, Validators.pattern(/\S/)]],
    billingBrand: [''], destinationExpenseReportId: ['', Validators.required],
  });

  constructor() {
    effect(() => this.reset());
  }

  reset(): void {
    const report = this.report();
    const expense = this.expense();

    const firstDay =
        `${String(report.year).padStart(4, '0')}-` +
        `${String(report.month).padStart(2, '0')}-01`;

    this.form.reset({
        date: expense?.date ?? firstDay,
        description: expense?.description ?? '',
        amount: expense?.amount ?? null,
        billingStreet: expense?.billingStreet ?? '',
        billingPostalCode: expense?.billingPostalCode ?? '',
        billingCity: expense?.billingCity ?? '',
        billingBrand: expense?.billingBrand ?? '',
        destinationExpenseReportId: report.id,
    });
  }

  submit(): void {
    if (this.form.invalid || this.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();

    if (values.amount === null) {
      return;
    }

    this.submitted.emit({
      expenseId: this.expense()?.id ?? null,
      request: {
        date: values.date,
        description: values.description.trim(),
        amount: values.amount,
        billingStreet: values.billingStreet.trim(),
        billingPostalCode: values.billingPostalCode.trim(),
        billingCity: values.billingCity.trim(),
        billingBrand: values.billingBrand.trim() || null,
        destinationExpenseReportId: values.destinationExpenseReportId,
      },
    });
  }
}