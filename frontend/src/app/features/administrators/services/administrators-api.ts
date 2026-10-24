import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import {
  AdministratorAccountResponse,
  CreateAdministratorRequest,
} from '../contracts/administrator-account';

@Injectable({
  providedIn: 'root',
})
export class AdministratorsApi {
  private readonly http = inject(HttpClient);

  create(request: CreateAdministratorRequest): Observable<AdministratorAccountResponse> {
    return this.http.post<AdministratorAccountResponse>(
      '/api/admin-accounts',
      request,
    );
  }
}