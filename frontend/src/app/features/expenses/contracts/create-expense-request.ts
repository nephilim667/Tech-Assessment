export interface CreateExpenseRequest {
  date: string;
  description: string;
  amount: number;
  billingStreet: string;
  billingPostalCode: string;
  billingCity: string;
  billingBrand: string | null;
}