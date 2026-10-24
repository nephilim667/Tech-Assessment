import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { of, Subject, throwError } from 'rxjs';

import { CreateUserRequest } from '../contracts/create-user-request';
import { UserResponse } from '../contracts/user-response';
import { UsersMessages } from '../errors/users-messages';
import { UsersApi } from '../services/users-api';
import { UsersStore } from './users-store';

describe('UsersStore', () => {
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

  const creationRequest: CreateUserRequest = {
    firstName: 'Juste',
    lastName: 'Leblanc',
    street: '10 rue des Lilas',
    postalCode: '75001',
    city: 'Paris',
    maxExpensesPerMonth: 5,
  };

  let api: {
    getAll: ReturnType<typeof vi.fn>;
    create: ReturnType<typeof vi.fn>;
    setStatus: ReturnType<typeof vi.fn>;
    delete: ReturnType<typeof vi.fn>;
  };

  let store: InstanceType<typeof UsersStore>;

  beforeEach(() => {
    api = {
      getAll: vi.fn(),
      create: vi.fn(),
      setStatus: vi.fn(),
      delete: vi.fn(),
    };

    TestBed.configureTestingModule({
      providers: [
        UsersStore,
        { provide: UsersApi, useValue: api },
      ],
    });

    store = TestBed.inject(UsersStore);
  });

  it('loads the users and exposes the loading state', async () => {
    const response = new Subject<UserResponse[]>();

    api.getAll.mockReturnValue(response);

    const loading = store.load();

    expect(store.isLoading()).toBe(true);

    response.next([user]);
    response.complete();

    await loading;

    expect(store.users()).toEqual([user]);
    expect(store.isLoading()).toBe(false);
    expect(store.error()).toBeNull();
  });

  it('preserves the existing list when loading fails', async () => {
    api.getAll.mockReturnValueOnce(of([user]));

    await store.load();

    api.getAll.mockReturnValueOnce(
      throwError(() => new HttpErrorResponse({ status: 503 })),
    );

    await store.load();

    expect(store.users()).toEqual([user]);
    expect(store.error()).toBe(UsersMessages.serviceUnavailable);
    expect(store.isLoading()).toBe(false);
  });

  it('adds the user returned by the API after creation', async () => {
    api.create.mockReturnValue(of(user));

    const success = await store.create(creationRequest);

    expect(success).toBe(true);
    expect(api.create).toHaveBeenCalledWith(creationRequest);
    expect(store.users()).toEqual([user]);
    expect(store.isSaving()).toBe(false);
  });

  it('replaces the user after a status change', async () => {
    api.getAll.mockReturnValue(of([user]));

    await store.load();

    const updatedUser: UserResponse = {
      ...user,
      isActive: false,
    };

    api.setStatus.mockReturnValue(of(updatedUser));

    const success = await store.setStatus(user.id, false);

    expect(success).toBe(true);
    expect(api.setStatus).toHaveBeenCalledWith(user.id, false);
    expect(store.users()).toEqual([updatedUser]);
  });

  it('removes the user only after successful deletion', async () => {
    api.getAll.mockReturnValue(of([user]));

    await store.load();

    const response = new Subject<void>();

    api.delete.mockReturnValue(response);

    const deletion = store.delete(user.id);

    expect(store.users()).toEqual([user]);
    expect(store.isSaving()).toBe(true);

    response.next();
    response.complete();

    expect(await deletion).toBe(true);
    expect(store.users()).toEqual([]);
    expect(store.isSaving()).toBe(false);
  });

  it('preserves the user and exposes the API message when deletion fails', async () => {
    api.getAll.mockReturnValue(of([user]));

    await store.load();

    api.delete.mockReturnValue(
      throwError(() => new HttpErrorResponse({
        status: 409,
        error: {
          title: 'Cette opération est interdite.',
          code: 'operation_forbidden',
        },
      })),
    );

    const success = await store.delete(user.id);

    expect(success).toBe(false);
    expect(store.users()).toEqual([user]);
    expect(store.error()).toBe('Cette opération est interdite.');
    expect(store.isSaving()).toBe(false);
  });

  it('prevents another mutation while a save is pending', async () => {
    const response = new Subject<UserResponse>();

    api.create.mockReturnValue(response);

    const creation = store.create(creationRequest);

    const secondResult = await store.setStatus(user.id, false);

    expect(secondResult).toBe(false);
    expect(api.setStatus).not.toHaveBeenCalled();

    response.next(user);
    response.complete();

    await creation;

    expect(store.isSaving()).toBe(false);
  });

  it('uses the fallback message and allows the error to be cleared', async () => {
    api.create.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: 500 })),
    );

    const success = await store.create(creationRequest);

    expect(success).toBe(false);
    expect(store.users()).toEqual([]);
    expect(store.error()).toBe(UsersMessages.createFailed);

    store.clearError();

    expect(store.error()).toBeNull();
  });
});