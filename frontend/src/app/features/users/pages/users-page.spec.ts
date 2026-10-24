import { signal } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { UserResponse } from '../contracts/user-response';
import { UsersStore } from '../store/users-store';
import { UsersPage } from './users-page';

describe('UsersPage', () => {
  const user: UserResponse = {
    id: '5a5617d5-f041-4a97-bfd0-32b187b10c4b',
    firstName: 'Juste',
    lastName: 'Leblanc',
    street: '10 rue des Lilas',
    postalCode: '75001',
    city: 'Paris',
    maxExpensesPerMonth: 5,
    isActive: true,
  };

  let fixture: ComponentFixture<UsersPage>;
  let page: UsersPage;

  let store: {
    users: ReturnType<typeof signal<UserResponse[]>>;
    isLoading: ReturnType<typeof signal<boolean>>;
    isSaving: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    load: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    setStatus: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  function fillValidForm(): void {
    page.form.setValue({
      firstName: ' Juste ',
      lastName: ' Leblanc ',
      street: ' 10 rue des Lilas ',
      postalCode: ' 75001 ',
      city: ' Paris ',
      maxExpensesPerMonth: 5,
    });
  }

  beforeEach(async () => {
    store = {
      users: signal([user]),
      isLoading: signal(false),
      isSaving: signal(false),
      error: signal<string | null>(null),
      load: vi.fn().mockResolvedValue(undefined),
      create: vi.fn().mockResolvedValue(true),
      setStatus: vi.fn().mockResolvedValue(true),
      delete: vi.fn().mockResolvedValue(true),
    };

    await TestBed.configureTestingModule({
      imports: [UsersPage],
    })
      .overrideComponent(UsersPage, {
        set: {
          providers: [
            { provide: UsersStore, useValue: store },
          ],
        },
      })
      .compileComponents();

    fixture = TestBed.createComponent(UsersPage);
    page = fixture.componentInstance;

    fixture.detectChanges();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads and displays the users when the page opens', () => {
    expect(store.load).toHaveBeenCalledOnce();

    const element = fixture.nativeElement as HTMLElement;

    expect(element.textContent).toContain('Juste Leblanc');
    expect(element.textContent).toContain('Paris');
    expect(element.textContent).toContain('Désactiver');
  });

  it('rejects an incomplete form', async () => {
    await page.submit();

    expect(store.create).not.toHaveBeenCalled();
    expect(page.form.controls.firstName.touched).toBe(true);
  });

  it.each([0, 1.5])(
    'rejects an invalid monthly quota: %s',
    async (quota) => {
      fillValidForm();

      page.form.controls.maxExpensesPerMonth.setValue(quota);

      await page.submit();

      expect(page.form.controls.maxExpensesPerMonth.invalid).toBe(true);
      expect(store.create).not.toHaveBeenCalled();
    },
  );

  it('creates the user with trimmed fields and resets the form', async () => {
    fillValidForm();

    await page.submit();

    expect(store.create).toHaveBeenCalledWith({
      firstName: 'Juste',
      lastName: 'Leblanc',
      street: '10 rue des Lilas',
      postalCode: '75001',
      city: 'Paris',
      maxExpensesPerMonth: 5,
    });

    expect(page.form.controls.firstName.value).toBe('');
    expect(page.form.controls.maxExpensesPerMonth.value).toBe(1);
    expect(page.form.pristine).toBe(true);
  });

  it('preserves the entered values when creation fails', async () => {
    store.create.mockResolvedValue(false);

    fillValidForm();

    await page.submit();

    expect(page.form.controls.firstName.value).toBe(' Juste ');
    expect(page.form.controls.maxExpensesPerMonth.value).toBe(5);
  });

  it('requests the opposite status for the selected user', async () => {
    await page.toggleStatus(user);

    expect(store.setStatus).toHaveBeenCalledWith(user.id, false);
  });

  it('does not delete the user when confirmation is cancelled', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(false);

    await page.deleteUser(user);

    expect(store.delete).not.toHaveBeenCalled();
  });

  it('deletes the selected user after confirmation', async () => {
    vi.spyOn(window, 'confirm').mockReturnValue(true);

    await page.deleteUser(user);

    expect(store.delete).toHaveBeenCalledWith(user.id);
  });
});