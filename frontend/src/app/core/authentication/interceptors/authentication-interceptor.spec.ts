import { TestBed } from '@angular/core/testing';
import {
  HttpClient,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { authenticationInterceptor } from './authentication-interceptor';
import { AuthenticationSession } from '../services/authentication-session';
import { AuthenticationResponse } from '../contracts/authentication-response';

describe('authenticationInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let session: AuthenticationSession;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(
          withInterceptors([authenticationInterceptor]),
        ),
        provideHttpClientTesting(),
      ],
    });

    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthenticationSession);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('adds the bearer token to API requests', () => {
    startSession();

    client.get('/api/users').subscribe();

    const pending = http.expectOne('/api/users');

    expect(pending.request.headers.get('Authorization'))
      .toBe('Bearer test-token');

    pending.flush([]);
  });

  it.each(['https://external.example/api/users', '/api-other/users',])
  ('does not send the token to %s', (url) => {
    startSession();

    client.get(url).subscribe();

    const pending = http.expectOne(url);

    expect(pending.request.headers.has('Authorization')).toBe(false);

    pending.flush({});
  });

  it('does not add a token to the login request', () => {
    startSession();

    client.post('/api/auth/login', {
      email: 'admin@example.test',
      password: 'test-password',
    }).subscribe();

    const pending = http.expectOne('/api/auth/login');

    expect(pending.request.headers.has('Authorization')).toBe(false);

    pending.flush({});
  });

  it('forwards requests without a token when there is no session', () => {
    client.get('/api/users').subscribe();

    const pending = http.expectOne('/api/users');

    expect(pending.request.headers.has('Authorization')).toBe(false);

    pending.flush([]);
  });

  it('discards an expired session before sending the request', () => {
    const response = startSession();

    vi.spyOn(Date, 'now').mockReturnValue(
      Date.parse(response.expiresAtUtc) + 1,
    );

    client.get('/api/users').subscribe();

    const pending = http.expectOne('/api/users');

    expect(pending.request.headers.has('Authorization')).toBe(false);
    expect(session.session()).toBeNull();

    pending.flush([]);
  });

  it('clears the current session when the API rejects its token', () => {
    startSession();

    client.get('/api/users').subscribe({ error: vi.fn() });

    const pending = http.expectOne('/api/users');

    pending.flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    expect(session.session()).toBeNull();
  });

  it('keeps the session when the API is temporarily unavailable', () => {
    startSession();

    client.get('/api/users').subscribe({ error: vi.fn() });

    const pending = http.expectOne('/api/users');

    pending.flush(null, {
      status: 503,
      statusText: 'Service Unavailable',
    });

    expect(session.getAccessToken()).toBe('test-token');
  });

  it('does not clear a newer session when an old request returns 401', () => {
    startSession('old-token');

    client.get('/api/users').subscribe({ error: vi.fn() });

    const pending = http.expectOne('/api/users');

    expect(pending.request.headers.get('Authorization'))
      .toBe('Bearer old-token');

    startSession('new-token');

    pending.flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    expect(session.getAccessToken()).toBe('new-token');
  });

  it('does not clear the session for an external service failure', () => {
    startSession();

    client.get('https://external.example/data')
      .subscribe({ error: vi.fn() });

    const pending = http.expectOne('https://external.example/data');

    pending.flush(null, {
      status: 401,
      statusText: 'Unauthorized',
    });

    expect(session.getAccessToken()).toBe('test-token');
  });

  function startSession(
    accessToken = 'test-token',
  ): AuthenticationResponse {
    const response: AuthenticationResponse = {
      accessToken,
      tokenType: 'Bearer',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    };

    session.start(response);
    return response;
  }
});