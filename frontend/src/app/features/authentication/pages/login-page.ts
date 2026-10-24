import { Component, inject } from '@angular/core';
import {
  NonNullableFormBuilder,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';

import { AuthenticationStore } from '../../../core/authentication/store/authentication-store';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
  styleUrl: '../login-page.css',
})
export class LoginPage {
  private readonly builder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly auth = inject(AuthenticationStore);

  readonly form = this.builder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  constructor() {
    this.auth.clearError();
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.auth.isPending()) {
      this.form.markAllAsTouched();
      return;
    }

    const { email, password } = this.form.getRawValue();

    const success = await this.auth.authenticate({
      email: email.trim(),
      password,
    });

    if (!success) {
      return;
    }

    this.form.controls.password.reset();

    const requested = this.route.snapshot.queryParamMap.get('returnUrl');

    const destination =
      requested?.startsWith('/') &&
      !requested.startsWith('//') &&
      !requested.startsWith('/login')
        ? requested
        : '/users';

    await this.router.navigateByUrl(destination);
  }
}