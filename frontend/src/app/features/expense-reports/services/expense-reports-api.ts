import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { ExpenseReportPeriod } from '../contracts/expense-report-period';
import { ExpenseReportResponse } from '../contracts/expense-report-response';

@Injectable({
  providedIn: 'root',
})
export class ExpenseReportsApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/expense-reports';

  getByPeriod(
    period: ExpenseReportPeriod,
  ): Observable<ExpenseReportResponse[]> {
    const params = new HttpParams()
      .set('userId', period.userId)
      .set('year', period.year)
      .set('month', period.month);

    return this.http.get<ExpenseReportResponse[]>(this.baseUrl, { params });
  }

  getById(id: string): Observable<ExpenseReportResponse> {
    return this.http.get<ExpenseReportResponse>(
      `${this.baseUrl}/${id}`,
    );
  }

  create(period: ExpenseReportPeriod): Observable<ExpenseReportResponse> {
    return this.http.post<ExpenseReportResponse>(
      this.baseUrl,
      period,
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${id}`,
    );
  }
}