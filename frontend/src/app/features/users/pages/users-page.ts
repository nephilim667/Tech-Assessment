import { Component, inject, OnInit } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { UsersStore } from '../store/users-store';
import { UserResponse } from '../contracts/user-response';

@Component({
  selector: 'app-users-page',
  imports: [ReactiveFormsModule],
  providers: [UsersStore],
  templateUrl: './users-page.html',
  styleUrl: '../users-styles.css',
})
export class UsersPage implements OnInit {
  private readonly builder = inject(NonNullableFormBuilder);

  readonly store = inject(UsersStore);

  readonly fields = [
    { name: 'firstName', label: 'Prénom', autocomplete: 'given-name' },
    { name: 'lastName', label: 'Nom', autocomplete: 'family-name' },
    { name: 'street', label: 'Adresse', autocomplete: 'street-address' },
    { name: 'postalCode', label: 'Code postal', autocomplete: 'postal-code' },
    { name: 'city', label: 'Ville', autocomplete: 'address-level2' },
  ] as const;

  readonly form = this.builder.group({
    firstName: ['', [Validators.required, Validators.pattern(/\S/)]],
    lastName: ['', [Validators.required, Validators.pattern(/\S/)]],
    street: ['', [Validators.required, Validators.pattern(/\S/)]],
    postalCode: ['', [Validators.required, Validators.pattern(/\S/)]],
    city: ['', [Validators.required, Validators.pattern(/\S/)]],
    maxExpensesPerMonth: [
      1,
      [
        Validators.required,
        Validators.min(1),
        Validators.pattern(/^\d+$/),
      ],
    ],
  });

  ngOnInit(): void {
    void this.store.load();
  }

  async submit(): Promise<void> {
    if (
      this.form.invalid ||
      this.store.isSaving() ||
      this.store.isLoading()
    ) {
      this.form.markAllAsTouched();
      return;
    }

    const values = this.form.getRawValue();

    const success = await this.store.create({
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim(),
      street: values.street.trim(),
      postalCode: values.postalCode.trim(),
      city: values.city.trim(),
      maxExpensesPerMonth: values.maxExpensesPerMonth,
    });

    if (success) {
      this.form.reset();
    }
  }

  async toggleStatus(user: UserResponse): Promise<void> {
    if (this.store.isSaving() || this.store.isLoading()) {
      return;
    }

    await this.store.setStatus(user.id, !user.isActive);
  }

  async deleteUser(user: UserResponse): Promise<void> {
    if (this.store.isSaving() || this.store.isLoading()) {
      return;
    }

    const confirmed = window.confirm(
      `Supprimer ${user.firstName} ${user.lastName} ? ` +
      'Ses notes de frais ne seront plus accessibles dans cette interface.',
    );

    if (confirmed) {
      await this.store.delete(user.id);
    }
  }
}