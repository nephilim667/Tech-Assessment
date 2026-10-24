import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { authenticationGuard } from './authentication-guard';
import { guestGuard } from './guest-guard';
import { AuthenticationSession } from '../services/authentication-session';

@Component({ template: 'Connexion' })
class LoginStub {}

@Component({ template: 'Utilisateurs' })
class UsersStub {}

describe('Authentication guards', () => {
  let session: AuthenticationSession;

  beforeEach(() => {
    sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          {
            path: 'login',
            component: LoginStub,
            canActivate: [guestGuard],
          },
          {
            path: 'users',
            component: UsersStub,
            canActivate: [authenticationGuard],
          },
        ]),
      ],
    });

    session = TestBed.inject(AuthenticationSession);
  });

  afterEach(() => {
    sessionStorage.clear();
    vi.restoreAllMocks();
  });

  it('redirects an anonymous visitor and preserves the requested URL', async () => {
    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/users', LoginStub);

    const router = TestBed.inject(Router);
    const url = router.parseUrl(router.url);

    expect(router.url.startsWith('/login')).toBe(true);
    expect(url.queryParams['returnUrl']).toBe('/users');
  });

  it('allows a visitor with a valid session', async () => {
    startSession();

    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/users', UsersStub);

    expect(TestBed.inject(Router).url).toBe('/users');
  });

  it('redirects an authenticated visitor away from login', async () => {
    startSession();

    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/login', UsersStub);

    expect(TestBed.inject(Router).url).toBe('/users');
  });

  it('rejects a session that has expired', async () => {
    const expiration = startSession();

    vi.spyOn(Date, 'now').mockReturnValue(expiration + 1);

    const harness = await RouterTestingHarness.create();

    await harness.navigateByUrl('/users', LoginStub);

    expect(session.session()).toBeNull();
  });

  function startSession(): number {
    const expiration = Date.now() + 3_600_000;

    session.start({
      accessToken: 'test-token',
      tokenType: 'Bearer',
      expiresAtUtc: new Date(expiration).toISOString(),
    });

    return expiration;
  }
});