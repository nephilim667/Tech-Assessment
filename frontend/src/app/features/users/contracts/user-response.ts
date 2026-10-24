export interface UserResponse {
  id: string;
  firstName: string;
  lastName: string;
  street: string;
  postalCode: string;
  city: string;
  maxExpensesPerMonth: number;
  isActive: boolean;
}