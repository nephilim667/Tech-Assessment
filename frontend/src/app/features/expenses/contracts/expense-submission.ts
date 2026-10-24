import { UpdateExpenseRequest } from './update-expense-request';

export interface ExpenseSubmission {
  expenseId: string | null;
  request: UpdateExpenseRequest;
}