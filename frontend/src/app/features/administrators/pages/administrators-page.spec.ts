import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdministratorsStore } from '../store/administrators-store';
import { AdministratorsPage } from './administrators-page';

describe('AdministratorsPage', () => {
  let fixture: ComponentFixture<AdministratorsPage>;
  let page: AdministratorsPage;

  let store: {
    isSaving: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    createdEmail: ReturnType<typeof signal<string | null>>;
    create: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    store = {
      isSaving: signal(false),
      error: signal<string | null>(null),
      createdEmail: signal<string | null>(null),
      create: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [AdministratorsPage],
    })
      .overrideComponent(AdministratorsPage, {
        set: {
          providers: [
            { provide: AdministratorsStore, useValue: store },
          ],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(AdministratorsPage);
    page = fixture.componentInstance;

    fixture.detectChanges();
  });

  it('rejects a weak password', async () => {
    page.form.setValue({
      email: 'comptable@example.test',
      password: 'weak',
    });

    await page.submit();

    expect(store.create).not.toHaveBeenCalled();
    expect(page.form.controls.password.invalid).toBe(true);
  });

  it('creates the account and clears the password after success', async () => {
    page.form.setValue({
      email: 'comptable@example.test',
      password: 'Test-Administrateur@2026!',
    });

    await page.submit();

    expect(store.create).toHaveBeenCalledWith({
      email: 'comptable@example.test',
      password: 'Test-Administrateur@2026!',
    });

    expect(page.form.controls.password.value).toBe('');
  });

  it('preserves the form when creation fails', async () => {
    store.create.mockResolvedValue(false);

    page.form.setValue({
      email: 'comptable@example.test',
      password: 'Test-Administrateur@2026!',
    });

    await page.submit();

    expect(page.form.controls.email.value).toBe('comptable@example.test');
    expect(page.form.controls.password.value).toBe(
      'Test-Administrateur@2026!',
    );
  });
});