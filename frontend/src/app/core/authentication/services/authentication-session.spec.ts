import { TestBed } from '@angular/core/testing';

import { AuthenticationSession } from './authentication-session';
import { AuthenticationResponse } from '../contracts/authentication-response';
import { AUTH_SESSION_STORAGE } from '../tokens/auth-session-storage';

describe('AuthenticationSession', () => {
  const storageKey = 'n2jsoft.auth.session';
  const now = Date.parse('2026-10-24T10:00:00Z');

  let session: AuthenticationSession;

  beforeEach(() => {
    sessionStorage.clear();
    vi.spyOn(Date, 'now').mockReturnValue(now);

    TestBed.configureTestingModule({
      providers: [
        {
          provide: AUTH_SESSION_STORAGE,
          useValue: sessionStorage,
        },
      ],
    });

    session = TestBed.inject(AuthenticationSession);
  });

  afterEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('stores a valid session and exposes its token', () => {
    const response = createResponse();

    session.start(response);

    expect(session.session()).toEqual(response);
    expect(session.getAccessToken()).toBe(response.accessToken);

    expect(
      JSON.parse(sessionStorage.getItem(storageKey)!),
    ).toEqual(response);
  });

  it('restores a valid session from storage', () => {
    const response = createResponse();
    session.start(response);

    const restored = createAnotherInstance();

    expect(restored.session()).toEqual(response);
    expect(restored.getAccessToken()).toBe(response.accessToken);
  });

  it('discards unreadable stored data', () => {
    sessionStorage.setItem(storageKey, '{invalid-json');

    const restored = createAnotherInstance();

    expect(restored.session()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('discards an expired stored session', () => {
    sessionStorage.setItem(
      storageKey,
      JSON.stringify({
        ...createResponse(),
        expiresAtUtc: new Date(now - 1).toISOString(),
      }),
    );

    const restored = createAnotherInstance();

    expect(restored.session()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('clears a session that expires before its next use', () => {
    session.start(createResponse());

    vi.mocked(Date.now).mockReturnValue(now + 3_600_001);

    expect(session.getAccessToken()).toBeNull();
    expect(session.session()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  it('clears only the authentication data', () => {
    sessionStorage.setItem('another-setting', 'preserved');
    session.start(createResponse());

    session.clear();

    expect(session.session()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
    expect(sessionStorage.getItem('another-setting')).toBe('preserved');
  });

  it('rejects an invalid authentication response without storing it', () => {
    expect(() =>
      session.start({
        ...createResponse(),
        accessToken: '',
      }),
    ).toThrow();

    expect(session.session()).toBeNull();
    expect(sessionStorage.getItem(storageKey)).toBeNull();
  });

  function createAnotherInstance(): AuthenticationSession {
    return TestBed.runInInjectionContext(
      () => new AuthenticationSession(),
    );
  }

  function createResponse(): AuthenticationResponse {
    return {
      accessToken: 'test-token',
      tokenType: 'Bearer',
      expiresAtUtc: new Date(now + 3_600_000).toISOString(),
    };
  }
});