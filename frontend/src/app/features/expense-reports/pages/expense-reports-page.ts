import { Component, inject, OnInit } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators} from '@angular/forms';
import { RouterLink } from '@angular/router';

import { UsersStore } from '../../users/store/users-store';
import { ExpenseReportResponse } from '../contracts/expense-report-response';
import { ExpenseReportsStore } from '../store/expense-reports-store';
import { MONTHS } from '../../../shared/constants/months';

@Component({
  selector: 'app-expense-reports-page',
  imports: [ReactiveFormsModule, RouterLink],
  providers: [UsersStore, ExpenseReportsStore],
  templateUrl: './expense-reports-page.html',
  styleUrl: '../expense-reports-styles.css',
})
export class ExpenseReportsPage implements OnInit {
  private readonly builder = inject(NonNullableFormBuilder);

  readonly usersStore = inject(UsersStore);
  readonly store = inject(ExpenseReportsStore);
  readonly months = MONTHS;

  private readonly today = new Date();

  readonly form = this.builder.group({
    userId: ['', Validators.required],
    year: [
      this.today.getFullYear(),
      [
        Validators.required,
        Validators.min(1),
        Validators.max(9999),
        Validators.pattern(/^\d+$/),
      ],
    ],
    month: [
      this.today.getMonth() + 1,
      [
        Validators.required,
        Validators.min(1),
        Validators.max(12),
        Validators.pattern(/^\d+$/),
      ],
    ],
  });

  ngOnInit(): void {
    void this.usersStore.load();
  }

  isBusy(): boolean {
    return this.store.isLoading() || this.store.isSaving();
  }

  matchesDisplayedPeriod(): boolean {
    const period = this.store.period();

    if (!period) {
      return false;
    }

    const values = this.form.getRawValue();

    return (
      values.userId === period.userId &&
      values.year === period.year &&
      values.month === period.month
    );
  }

  displayedUserName(): string {
    const userId = this.store.period()?.userId;
    const user = this.usersStore.users().find(
      (candidate) => candidate.id === userId,
    );

    return user ? `${user.firstName} ${user.lastName}` : '';
  }

  canCreate(): boolean {
    if (this.isBusy() || !this.matchesDisplayedPeriod()) {
      return false;
    }

    const userId = this.store.period()?.userId;

    return this.usersStore.users().some(
      (user) => user.id === userId && user.isActive,
    );
  }

  async search(): Promise<void> {
    if (this.isBusy() || this.usersStore.isLoading()) {
      return;
    }

    if (this.form.invalid) {
      this.store.resetSelection();
      return;
    }

    await this.store.load(this.form.getRawValue());
  }

  async createReport(): Promise<void> {
    if (this.canCreate()) {
      await this.store.create();
    }
  }

  async deleteReport(report: ExpenseReportResponse): Promise<void> {
    if (this.isBusy()) {
      return;
    }

    const confirmed = window.confirm(
      `Supprimer la note « ${report.title} » ` +
      `(référence : ${report.reference}) ? ` +
      'Toutes ses dépenses seront définitivement supprimées.',
    );

    if (confirmed) {
      await this.store.delete(report.id);
    }
  }
}