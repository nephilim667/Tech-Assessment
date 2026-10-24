import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AuthenticationApi } from './authentication-api';
import { AuthenticationResponse } from '../contracts/authentication-response';

describe('AuthenticationApi', () => {
  let api: AuthenticationApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(AuthenticationApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('authenticates with the API and returns the token response', () => {
    const request = {
      email: 'admin@example.test',
      password: 'Test-Password@2026!',
    };

    const response: AuthenticationResponse = {
      accessToken: 'test-token',
      tokenType: 'Bearer',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    };

    const received = vi.fn();

    api.authenticate(request).subscribe(received);

    const pending = http.expectOne('/api/auth/login');

    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toEqual(request);

    pending.flush(response);

    expect(received).toHaveBeenCalledWith(response);
  });

  it('preserves authentication failures for the calling layer', () => {
    const failed = vi.fn();

    api.authenticate({
      email: 'admin@example.test',
      password: 'incorrect',
    }).subscribe({ error: failed });

    const pending = http.expectOne('/api/auth/login');

    const problem = {
      status: 401,
      code: 'auth.invalid_credentials',
    };

    pending.flush(problem, {
      status: 401,
      statusText: 'Unauthorized',
    });

    expect(failed).toHaveBeenCalledWith(
      expect.objectContaining({
        status: 401,
        error: problem,
      }),
    );
  });

  it('requests server-side logout', () => {
    const completed = vi.fn();

    api.logout().subscribe(completed);

    const pending = http.expectOne('/api/auth/logout');

    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toBeNull();

    pending.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    expect(completed).toHaveBeenCalledTimes(1);
  });
});