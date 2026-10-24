import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { ExpenseReportPeriod } from '../contracts/expense-report-period';
import { ExpenseReportResponse } from '../contracts/expense-report-response';
import { ExpenseReportsApi } from './expense-reports-api';

describe('ExpenseReportsApi', () => {
  let api: ExpenseReportsApi;
  let http: HttpTestingController;

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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(ExpenseReportsApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets the reports for the selected user and period', () => {
    let result: ExpenseReportResponse[] | undefined;

    api.getByPeriod(period).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne(
      (candidate) => candidate.url === '/api/expense-reports',
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('userId')).toBe(period.userId);
    expect(request.request.params.get('year')).toBe('2026');
    expect(request.request.params.get('month')).toBe('10');

    request.flush([report]);

    expect(result).toEqual([report]);
  });

  it('gets a report by its identifier', () => {
    let result: ExpenseReportResponse | undefined;

    api.getById(report.id).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne(
      `/api/expense-reports/${report.id}`,
    );

    expect(request.request.method).toBe('GET');

    request.flush(report);

    expect(result).toEqual(report);
  });

  it('creates a report for the selected user and period', () => {
    let result: ExpenseReportResponse | undefined;

    api.create(period).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne('/api/expense-reports');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(period);

    request.flush(report, {
      status: 201,
      statusText: 'Created',
    });

    expect(result).toEqual(report);
  });

  it('deletes the selected report', () => {
    let completed = false;

    api.delete(report.id).subscribe({
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne(
      `/api/expense-reports/${report.id}`,
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    expect(completed).toBe(true);
  });

  it('propagates a creation refusal from the API', () => {
    let receivedStatus: number | undefined;

    api.create(period).subscribe({
      error: (error) => {
        receivedStatus = error.status;
      },
    });

    const request = http.expectOne('/api/expense-reports');

    request.flush(
      {
        title: 'Création interdite pour cet utilisateur.',
        code: 'user_inactive',
      },
      {
        status: 409,
        statusText: 'Conflict',
      },
    );

    expect(receivedStatus).toBe(409);
  });
});