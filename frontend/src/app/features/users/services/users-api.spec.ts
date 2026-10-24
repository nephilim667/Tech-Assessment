import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';

import { CreateUserRequest } from '../contracts/create-user-request';
import { UserResponse } from '../contracts/user-response';
import { UsersApi } from './users-api';

describe('UsersApi', () => {
  let api: UsersApi;
  let http: HttpTestingController;

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

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    api = TestBed.inject(UsersApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('returns the users', () => {
    let result: UserResponse[] | undefined;

    api.getAll().subscribe((response) => {
      result = response;
    });

    const request = http.expectOne('/api/users');

    expect(request.request.method).toBe('GET');

    request.flush([user]);

    expect(result).toEqual([user]);
  });

  it('creates a user with the supplied data', () => {
    const data: CreateUserRequest = {
      firstName: 'Juste',
      lastName: 'Leblanc',
      street: '10 rue des Lilas',
      postalCode: '75001',
      city: 'Paris',
      maxExpensesPerMonth: 5,
    };

    let result: UserResponse | undefined;

    api.create(data).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne('/api/users');

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(data);

    request.flush(user, {
      status: 201,
      statusText: 'Created',
    });

    expect(result).toEqual(user);
  });

  it('changes the user status and returns the updated user', () => {
    const updatedUser: UserResponse = {
      ...user,
      isActive: false,
    };

    let result: UserResponse | undefined;

    api.setStatus(user.id, false).subscribe((response) => {
      result = response;
    });

    const request = http.expectOne(
      `/api/users/${user.id}/status`,
    );

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      isActive: false,
    });

    request.flush(updatedUser);

    expect(result).toEqual(updatedUser);
  });

  it('deletes the user', () => {
    let completed = false;

    api.delete(user.id).subscribe({
      complete: () => {
        completed = true;
      },
    });

    const request = http.expectOne(
      `/api/users/${user.id}`,
    );

    expect(request.request.method).toBe('DELETE');

    request.flush(null, {
      status: 204,
      statusText: 'No Content',
    });

    expect(completed).toBe(true);
  });

  it('propagates an API error to the caller', () => {
    let receivedStatus: number | undefined;

    api.getAll().subscribe({
      error: (error) => {
        receivedStatus = error.status;
      },
    });

    const request = http.expectOne('/api/users');

    request.flush(
      { title: 'Service indisponible' },
      {
        status: 503,
        statusText: 'Service Unavailable',
      },
    );

    expect(receivedStatus).toBe(503);
  });
});