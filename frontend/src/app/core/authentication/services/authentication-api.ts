import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { AuthenticationRequest } from '../contracts/authentication-request';
import { AuthenticationResponse } from '../contracts/authentication-response';

@Injectable({ providedIn: 'root' })
export class AuthenticationApi {
  private readonly http = inject(HttpClient);

  authenticate(request: AuthenticationRequest): Observable<AuthenticationResponse> {
    return this.http.post<AuthenticationResponse>('/api/auth/login',request);
  }

  logout(): Observable<void> {
    return this.http.post<void>('/api/auth/logout', null);
  }
}