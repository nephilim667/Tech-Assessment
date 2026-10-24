import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { CreateExpenseRequest } from '../contracts/create-expense-request';
import { UpdateExpenseRequest } from '../contracts/update-expense-request';
import { ExpenseResponse } from '../contracts/expense-response';
import { ExpensePageResponse } from '../contracts/expense-page-response';
import { ExpensesApi } from './expenses-api';

describe('ExpensesApi', () => {
  const reportId = '05631031-9ff0-4bd5-9c2d-03f2f7879efe';
  const destinationId = 'b6c84430-923b-4ad3-a992-a94eca88914b';
  const baseUrl = `/api/expense-reports/${reportId}/expenses`;

  const data: CreateExpenseRequest = {
    date: '2026-10-24',
    description: 'Déjeuner professionnel',
    amount: 25.5,
    billingStreet: '10 rue des Lilas',
    billingPostalCode: '75001',
    billingCity: 'Paris',
    billingBrand: null,
  };

  const expense: ExpenseResponse = {
    ...data,
    id: '19d4a834-a294-4d6e-9dc2-fbddfa77b32b',
    expenseReportId: reportId,
  };

  let api: ExpensesApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(ExpensesApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('gets the requested page and the totals returned by the API', () => {
    const page: ExpensePageResponse = {
      items: [expense],
      pageNumber: 2,
      pageSize: 5,
      totalCount: 6,
      totalPages: 2,
      totalAmount: 150,
    };

    let result: ExpensePageResponse | undefined;

    api.getPage(reportId, 2).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne(
      (candidate) => candidate.url === baseUrl,
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('page')).toBe('2');

    request.flush(page);

    expect(result).toEqual(page);
    expect(result?.totalAmount).toBe(150);
  });

  it('requests the first page by default', () => {
    api.getPage(reportId).subscribe();

    const request = http.expectOne(
      (candidate) => candidate.url === baseUrl,
    );

    expect(request.request.params.get('page')).toBe('1');

    request.flush({
      items: [],
      pageNumber: 1,
      pageSize: 5,
      totalCount: 0,
      totalPages: 0,
      totalAmount: 0,
    });
  });

  it('creates an expense in the selected report', () => {
    let result: ExpenseResponse | undefined;

    api.create(reportId, data).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne(baseUrl);

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(data);

    request.flush(expense, {
      status: 201,
      statusText: 'Created',
    });

    expect(result).toEqual(expense);
  });

  it('updates an expense using its source report and destination', () => {
    const update: UpdateExpenseRequest = {
      ...data,
      destinationExpenseReportId: destinationId,
      description: 'Description corrigée',
    };

    const updated: ExpenseResponse = {
      ...expense,
      expenseReportId: destinationId,
      description: update.description,
    };

    let result: ExpenseResponse | undefined;

    api.update(reportId, expense.id, update).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne(`${baseUrl}/${expense.id}`);

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(update);

    request.flush(updated);

    expect(result).toEqual(updated);
  });

  it('deletes the selected expense', () => {
    let completed = false;

    api.delete(reportId, expense.id).subscribe({
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne(`${baseUrl}/${expense.id}`);

    expect(request.request.method).toBe('DELETE');

    request.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    expect(completed).toBe(true);
  });

  it('propagates a refusal from the API', () => {
    let receivedStatus: number | undefined;

    api.create(reportId, data).subscribe({
      error: (error) => {
        receivedStatus = error.status;
      },
    });

    const request = http.expectOne(baseUrl);

    request.flush(
      { title: 'Opération refusée.' },
      {
        status: 409,
        statusText: 'Conflict',
      },
    );

    expect(receivedStatus).toBe(409);
  });
});