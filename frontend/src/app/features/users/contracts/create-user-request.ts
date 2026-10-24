export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  street: string;
  postalCode: string;
  city: string;
  maxExpensesPerMonth: number;
}