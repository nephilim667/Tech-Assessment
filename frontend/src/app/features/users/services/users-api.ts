import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { CreateUserRequest } from '../contracts/create-user-request';
import { UserResponse } from '../contracts/user-response';

@Injectable({
  providedIn: 'root',
})
export class UsersApi {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/users';

  getAll(): Observable<UserResponse[]> {
    return this.http.get<UserResponse[]>(this.baseUrl);
  }

  create(request: CreateUserRequest): Observable<UserResponse> {
    return this.http.post<UserResponse>(this.baseUrl,request);
  }

  setStatus(id: string, isActive: boolean): Observable<UserResponse> {
    return this.http.put<UserResponse>(
      `${this.baseUrl}/${id}/status`,
      { isActive },
    );
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${id}`,
    );
  }
}