import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateExpenseRequest } from '../contracts/create-expense-request';
import { UpdateExpenseRequest } from '../contracts/update-expense-request';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpensePageResponse } from '../contracts/expense-page-response';

@Injectable({providedIn: 'root'})
export class ExpensesApi {
  private readonly http = inject(HttpClient);

  getPage(reportId: string, page = 1): Observable<ExpensePageResponse> {
    const params = new HttpParams().set('page', page);

    return this.http.get<ExpensePageResponse>(
      this.getBaseUrl(reportId),
      { params },
    );
  }

  create(reportId: string, request: CreateExpenseRequest): Observable<ExpenseResponse> {
    return this.http.post<ExpenseResponse>(
      this.getBaseUrl(reportId),
      request,
    );
  }

  update(sourceReportId: string, expenseId: string, request: UpdateExpenseRequest): Observable<ExpenseResponse> {
    return this.http.put<ExpenseResponse>(
      `${this.getBaseUrl(sourceReportId)}/${expenseId}`,
      request,
    );
  }

  delete(reportId: string, expenseId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.getBaseUrl(reportId)}/${expenseId}`,
    );
  }

  private getBaseUrl(reportId: string): string {
    return `/api/expense-reports/${reportId}/expenses`;
  }
}