import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, inject, OnInit, signal, viewChild } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ExpenseReportsStore } from '../../expense-reports/store/expense-reports-store';
import { ExpenseForm } from '../components/expense-form';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpenseSubmission } from '../contracts/expense-submission';
import { ExpensesStore } from '../store/expenses-store';
import { MONTHS } from '../../../shared/constants/months';

@Component({
  selector: 'app-expenses-page',
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    ExpenseForm,
  ],
  providers: [ExpensesStore, ExpenseReportsStore],
  templateUrl: './expenses-page.html',
  styleUrl: '../expenses-styles.css',
})
export class ExpensesPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly builder = inject(NonNullableFormBuilder);

  readonly store = inject(ExpensesStore);
  readonly destinationsStore = inject(ExpenseReportsStore);
  readonly months = MONTHS;

  readonly editor = viewChild(ExpenseForm);
  readonly editing = signal<ExpenseResponse | null>(null);

  readonly destinationForm = this.builder.group({
    year: [
      new Date().getFullYear(),
      [
        Validators.required,
        Validators.min(1),
        Validators.max(9999),
        Validators.pattern(/^\d+$/),
      ],
    ],
    month: [
      new Date().getMonth() + 1,
      [
        Validators.required,
        Validators.min(1),
        Validators.max(12),
        Validators.pattern(/^\d+$/),
      ],
    ],
  });

  ngOnInit(): void {
    const reportId = this.route.snapshot.paramMap.get('expenseReportId');

    if (reportId) {
      void this.store.load(reportId);
    }
  }

  isBusy(): boolean {
    return (
      this.store.isLoading() ||
      this.store.isSaving() ||
      this.destinationsStore.isLoading()
    );
  }

  async save(submission: ExpenseSubmission): Promise<void> {
    if (this.isBusy()) {
      return;
    }

    if (submission.expenseId !== null) {
      const success = await this.store.update(
        submission.expenseId,
        submission.request,
      );

      if (success) {
        this.editing.set(null);
      }

      return;
    }

    const {
      destinationExpenseReportId,
      ...creationRequest
    } = submission.request;

    // La création appartient toujours à la note affichée.
    const report = this.store.report();

    if (!report || destinationExpenseReportId !== report.id) {
      return;
    }

    const success = await this.store.create(creationRequest);

    if (success) {
      this.editor()?.reset();
    }
  }

  async editExpense(expense: ExpenseResponse): Promise<void> {
    const report = this.store.report();

    if (!report || this.isBusy()) {
      return;
    }

    this.editing.set(expense);

    this.destinationForm.setValue({
      year: report.year,
      month: report.month,
    });

    await this.loadDestinations();
  }

  cancelEdit(): void {
    if (!this.isBusy()) {
      this.editing.set(null);
    }
  }

  async loadDestinations(): Promise<void> {
    const report = this.store.report();

    if (!report || !this.editing() || this.isBusy()) {
      return;
    }

    // Une nouvelle recherche ne conserve pas une ancienne destination.
    this.editor()?.form.controls.destinationExpenseReportId
      .setValue(report.id);

    if (this.destinationForm.invalid) {
      this.destinationsStore.resetSelection();
      return;
    }

    await this.destinationsStore.load({
      userId: report.userId,
      ...this.destinationForm.getRawValue(),
    });
  }

  async deleteExpense(expense: ExpenseResponse): Promise<void> {
    if (this.isBusy()) {
      return;
    }

    const confirmed = window.confirm(
      `Supprimer la dépense « ${expense.description} » ?`,
    );

    if (!confirmed) {
      return;
    }

    const success = await this.store.delete(expense.id);

    if (success && this.editing()?.id === expense.id) {
      this.editing.set(null);
    }
  }
}