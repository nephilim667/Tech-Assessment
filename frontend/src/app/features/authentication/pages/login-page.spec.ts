import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';

import { LoginPage } from './login-page';
import { AuthenticationStore } from '../../../core/authentication/store/authentication-store';

@Component({ template: 'Utilisateurs' })
class UsersStub {}

describe('LoginPage', () => {
  const auth = {
    isPending: signal(false),
    error: signal<string | null>(null),
    clearError: vi.fn(),
    authenticate: vi.fn(),
  };

  beforeEach(() => {
    auth.isPending.set(false);
    auth.error.set(null);
    auth.clearError.mockReset();
    auth.authenticate.mockReset();

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthenticationStore, useValue: auth },
        provideRouter([
          { path: 'login', component: LoginPage },
          { path: 'users', component: UsersStub },
        ]),
      ],
    });
  });

  it('does not authenticate an invalid form', async () => {
    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/login', LoginPage);

    await page.submit();

    expect(auth.authenticate).not.toHaveBeenCalled();
    expect(page.form.controls.email.touched).toBe(true);
    expect(page.form.controls.password.touched).toBe(true);
  });

  it('authenticates and navigates to the application', async () => {
    auth.authenticate.mockResolvedValue(true);

    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/login', LoginPage);

    page.form.setValue({
      email: 'admin@example.test',
      password: 'test-password',
    });

    await page.submit();

    expect(auth.authenticate).toHaveBeenCalledWith({
      email: 'admin@example.test',
      password: 'test-password',
    });

    expect(page.form.controls.password.value).toBe('');
    expect(TestBed.inject(Router).url).toBe('/users');
  });

  it('displays the authentication error and stays on login', async () => {
    auth.authenticate.mockResolvedValue(false);

    const harness = await RouterTestingHarness.create();
    const page = await harness.navigateByUrl('/login', LoginPage);

    page.form.setValue({
      email: 'admin@example.test',
      password: 'incorrect',
    });

    auth.error.set('Identifiants incorrects.');

    await page.submit();
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe('/login');
    expect(harness.routeNativeElement?.textContent)
      .toContain('Identifiants incorrects.');
  });

  it('does not use an external return URL', async () => {
    auth.authenticate.mockResolvedValue(true);

    const harness = await RouterTestingHarness.create();

    const page = await harness.navigateByUrl(
      '/login?returnUrl=https%3A%2F%2Fexternal.example',
      LoginPage,
    );

    page.form.setValue({
      email: 'admin@example.test',
      password: 'test-password',
    });

    await page.submit();

    expect(TestBed.inject(Router).url).toBe('/users');
  });
});