export interface CreateAdministratorRequest {
  email: string;
  password: string;
}

export interface AdministratorAccountResponse {
  id: string;
  email: string;
}