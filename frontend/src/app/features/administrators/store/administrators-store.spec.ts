import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { AdministratorsStore } from './administrators-store';

describe('AdministratorsStore', () => {
  const request = {
    email: 'comptable@example.test',
    password: 'Test-Administrateur@2026!',
  };

  let store: InstanceType<typeof AdministratorsStore>;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AdministratorsStore,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    store = TestBed.inject(AdministratorsStore);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('creates the account and records the returned email', async () => {
    const creation = store.create(request);

    expect(store.isSaving()).toBe(true);

    const pending = http.expectOne('/api/admin-accounts');

    expect(pending.request.method).toBe('POST');
    expect(pending.request.body).toEqual(request);

    pending.flush(
      { id: 'administrator-id', email: request.email },
      { status: 201, statusText: 'Created' },
    );

    expect(await creation).toBe(true);
    expect(store.createdEmail()).toBe(request.email);
    expect(store.error()).toBeNull();
    expect(store.isSaving()).toBe(false);
  });

  it('displays a duplicate-email refusal', async () => {
    const creation = store.create(request);

    http.expectOne('/api/admin-accounts').flush(
      { title: 'Cette adresse e-mail est déjà utilisée.' },
      { status: 409, statusText: 'Conflict' },
    );

    expect(await creation).toBe(false);
    expect(store.createdEmail()).toBeNull();
    expect(store.error()).toBe(
      'Cette adresse e-mail est déjà utilisée.',
    );
    expect(store.isSaving()).toBe(false);
  });

  it('prevents duplicate submissions while creation is pending', async () => {
    const creation = store.create(request);

    expect(await store.create(request)).toBe(false);

    const pending = http.expectOne('/api/admin-accounts');

    pending.flush(
      { id: 'administrator-id', email: request.email },
      { status: 201, statusText: 'Created' },
    );

    await creation;
  });
});