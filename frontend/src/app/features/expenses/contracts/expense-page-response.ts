import { ExpenseResponse } from './expense-response';

export interface ExpensePageResponse {
  items: ExpenseResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  totalAmount: number;
}