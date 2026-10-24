import { inject, Injectable, signal, computed } from '@angular/core';

import { hasJwtRole } from '../helpers/has-jwt-role';
import { AuthenticationResponse } from '../contracts/authentication-response';
import { AUTH_SESSION_STORAGE } from '../tokens/auth-session-storage';

@Injectable({ providedIn: 'root' })
export class AuthenticationSession {
  private readonly storage = inject(AUTH_SESSION_STORAGE);
  private readonly storageKey = 'n2jsoft.auth.session';

  private readonly currentSession = signal<AuthenticationResponse | null>(this.restore());
  readonly session = this.currentSession.asReadonly();

  readonly isSuperAdmin = computed(() =>
    hasJwtRole(
      this.session()?.accessToken ?? null,
      'SuperAdmin',
    ),
  );

  start(response: AuthenticationResponse): void {
    if (!this.isValid(response)) {
      throw new Error("La réponse d’authentification est invalide ou expirée.");
    }

    this.storage.setItem(this.storageKey, JSON.stringify(response));
    this.currentSession.set(response);
  }

  getAccessToken(): string | null {
    const current = this.currentSession();

    if (!current) {
      return null;
    }

    if (this.isValid(current)) {
      return current.accessToken;
    }

    this.clear();
    return null;
  }

  clear(): void {
    this.storage.removeItem(this.storageKey);
    this.currentSession.set(null);
  }

  private restore(): AuthenticationResponse | null {
    const stored = this.storage.getItem(this.storageKey);

    if (!stored) {
      return null;
    }

    try {
      const value: unknown = JSON.parse(stored);

      if (this.isValid(value)) {
        return value;
      }
    } catch {
      // Une donnée locale illisible ne doit pas empêcher le démarrage.
    }

    this.storage.removeItem(this.storageKey);
    return null;
  }

  private isValid(value: unknown): value is AuthenticationResponse {
    if (!value || typeof value !== 'object') {
      return false;
    }

    const candidate = value as Partial<AuthenticationResponse>;

    return (
      typeof candidate.accessToken === 'string' &&
      candidate.accessToken.trim().length > 0 &&
      candidate.tokenType === 'Bearer' &&
      typeof candidate.expiresAtUtc === 'string' &&
      Number.isFinite(Date.parse(candidate.expiresAtUtc)) &&
      Date.parse(candidate.expiresAtUtc) > Date.now()
    );
  }
}