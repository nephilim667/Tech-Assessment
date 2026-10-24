import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AdministratorsStore } from '../store/administrators-store';

@Component({
  selector: 'app-administrators-page',
  imports: [ReactiveFormsModule],
  providers: [AdministratorsStore],
  templateUrl: './administrators-page.html',
  styleUrl: './administrators-page.css',
})
export class AdministratorsPage {
  private readonly builder = inject(NonNullableFormBuilder);

  readonly store = inject(AdministratorsStore);

  readonly form = this.builder.group({
    email: ['', [Validators.required, Validators.email]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(12),
        Validators.pattern(/[A-Z]/),
        Validators.pattern(/[a-z]/),
        Validators.pattern(/[0-9]/),
        Validators.pattern(/[^a-zA-Z0-9]/),
      ],
    ],
  });

  async submit(): Promise<void> {
    if (this.form.invalid || this.store.isSaving()) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();

    const success = await this.store.create({
      email: values.email.trim(),
      password: values.password,
    });

    if (success) {
      this.form.reset();
    }
  }
}