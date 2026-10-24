export interface ExpenseResponse {
  id: string;
  expenseReportId: string;
  date: string;
  description: string;
  amount: number;
  billingStreet: string;
  billingPostalCode: string;
  billingCity: string;
  billingBrand: string | null;
}