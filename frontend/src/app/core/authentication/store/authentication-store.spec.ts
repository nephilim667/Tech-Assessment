import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';

import { AuthenticationStore } from './authentication-store';
import { AuthenticationApi } from '../services/authentication-api';
import { AuthenticationSession } from '../services/authentication-session';
import { AuthenticationResponse } from '../contracts/authentication-response';
import { AuthenticationMessages } from '../errors/authentication-messages';

describe('AuthenticationStore', () => {
  let store: InstanceType<typeof AuthenticationStore>;
  let session: AuthenticationSession;

  const api = {
    authenticate: vi.fn(),
    logout: vi.fn(),
  };

  const request = {
    email: 'admin@example.test',
    password: 'Test-Password@2026!',
  };

  beforeEach(() => {
    sessionStorage.clear();
    api.authenticate.mockReset();
    api.logout.mockReset();

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthenticationApi, useValue: api },
      ],
    });

    store = TestBed.inject(AuthenticationStore);
    session = TestBed.inject(AuthenticationSession);
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('stores the session after successful authentication', async () => {
    const response = createResponse();
    api.authenticate.mockReturnValue(of(response));

    const success = await store.authenticate(request);

    expect(success).toBe(true);
    expect(api.authenticate).toHaveBeenCalledWith(request);
    expect(session.session()).toEqual(response);
    expect(store.hasSession()).toBe(true);
    expect(store.isPending()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('exposes an authentication failure without creating a session', async () => {
    api.authenticate.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 401 })),
    );

    const success = await store.authenticate(request);

    expect(success).toBe(false);
    expect(session.session()).toBeNull();
    expect(store.error()).toBe(
      AuthenticationMessages.invalidCredentials,
    );
    expect(store.isPending()).toBe(false);
  });

  it('reports service unavailability during authentication', async () => {
    api.authenticate.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    const success = await store.authenticate(request);

    expect(success).toBe(false);
    expect(store.error()).toBe(
      AuthenticationMessages.serviceUnavailable,
    );
    expect(store.isPending()).toBe(false);
  });

  it('prevents a second authentication while one is pending', async () => {
    const response = new Subject<AuthenticationResponse>();
    api.authenticate.mockReturnValue(response);

    const first = store.authenticate(request);

    expect(store.isPending()).toBe(true);
    expect(await store.authenticate(request)).toBe(false);
    expect(api.authenticate).toHaveBeenCalledTimes(1);

    response.next(createResponse());
    response.complete();

    expect(await first).toBe(true);
    expect(store.isPending()).toBe(false);
  });

  it('keeps the session until server-side logout succeeds', async () => {
    session.start(createResponse());

    const response = new Subject<void>();
    api.logout.mockReturnValue(response);

    const pending = store.logout();

    expect(store.isPending()).toBe(true);
    expect(session.getAccessToken()).toBe('test-token');

    response.next();
    response.complete();

    expect(await pending).toBe(true);
    expect(api.logout).toHaveBeenCalledTimes(1);
    expect(session.session()).toBeNull();
    expect(store.hasSession()).toBe(false);
    expect(store.isPending()).toBe(false);
  });

  it('keeps the session when server-side logout fails', async () => {
    session.start(createResponse());

    api.logout.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    const success = await store.logout();

    expect(success).toBe(false);
    expect(session.getAccessToken()).toBe('test-token');
    expect(store.error()).toBe(
      AuthenticationMessages.serviceUnavailable,
    );
    expect(store.isPending()).toBe(false);
  });

  it('clears the session when logout reports an invalid token', async () => {
    session.start(createResponse());

    api.logout.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 401 })),
    );

    const success = await store.logout();

    expect(success).toBe(true);
    expect(session.session()).toBeNull();
    expect(store.error()).toBeNull();
    expect(store.isPending()).toBe(false);
  });

  it('does not call logout when there is no usable session', async () => {
    const success = await store.logout();

    expect(success).toBe(true);
    expect(api.logout).not.toHaveBeenCalled();
    expect(session.session()).toBeNull();
    expect(store.isPending()).toBe(false);
  });

  function createResponse(): AuthenticationResponse {
    return {
      accessToken: 'test-token',
      tokenType: 'Bearer',
      expiresAtUtc: new Date(Date.now() + 3_600_000).toISOString(),
    };
  }
});