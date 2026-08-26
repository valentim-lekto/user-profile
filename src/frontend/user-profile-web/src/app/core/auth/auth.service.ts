import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export const AUTH_TOKEN_STORAGE_KEY = 'user-profile.access-token';

export interface LoginRequest {
  email: string;
  password: string;
}

interface LoginResponse {
  accessToken: string;
}

export interface AuthProblemDetails {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly accessToken = signal<string | null>(null);

  readonly loading = signal(false);
  readonly error = signal<AuthProblemDetails | null>(null);

  constructor() {
    this.getValidAccessToken();
  }

  async login(request: LoginRequest): Promise<boolean> {
    if (this.loading()) {
      return false;
    }

    this.loading.set(true);
    this.error.set(null);

    try {
      const response = await firstValueFrom(
        this.http.post<LoginResponse>('/api/auth/login', request),
      );

      if (!isUnexpiredJwt(response.accessToken)) {
        this.clearSession();
        this.error.set({ status: 0 });
        return false;
      }

      sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, response.accessToken);
      this.accessToken.set(response.accessToken);
      return true;
    } catch (error: unknown) {
      this.error.set(toProblemDetails(error));
      return false;
    } finally {
      this.loading.set(false);
    }
  }

  getValidAccessToken(): string | null {
    const storedToken = sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY);

    if (!isUnexpiredJwt(storedToken)) {
      this.clearSession();
      return null;
    }

    if (this.accessToken() !== storedToken) {
      this.accessToken.set(storedToken);
    }

    return storedToken;
  }

  hasValidSession(): boolean {
    return this.getValidAccessToken() !== null;
  }

  isCurrentAccessToken(accessToken: string): boolean {
    return sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY) === accessToken;
  }

  clearSession(): void {
    sessionStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
    this.accessToken.set(null);
  }
}

function isUnexpiredJwt(value: unknown): value is string {
  if (typeof value !== 'string') {
    return false;
  }

  const parts = value.split('.');
  if (parts.length !== 3 || parts.some((part) => part.length === 0)) {
    return false;
  }

  try {
    const encodedPayload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const paddedPayload = encodedPayload.padEnd(Math.ceil(encodedPayload.length / 4) * 4, '=');
    const payload: unknown = JSON.parse(atob(paddedPayload));

    if (!isRecord(payload)) {
      return false;
    }

    const expiresAt = payload['exp'];
    return (
      typeof expiresAt === 'number' &&
      Number.isSafeInteger(expiresAt) &&
      expiresAt > Math.floor(Date.now() / 1000)
    );
  } catch {
    return false;
  }
}

function toProblemDetails(error: unknown): AuthProblemDetails {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0 };
  }

  if (!isRecord(error.error)) {
    return { status: error.status };
  }

  return {
    status: typeof error.error['status'] === 'number' ? error.error['status'] : error.status,
    title: readString(error.error['title']),
    detail: readString(error.error['detail']),
    errors: readValidationErrors(error.error['errors']),
  };
}

function readString(value: unknown): string | undefined {
  return typeof value === 'string' ? value : undefined;
}

function readValidationErrors(value: unknown): Record<string, string[]> | undefined {
  if (!isRecord(value)) {
    return undefined;
  }

  const errors = Object.entries(value).flatMap(([field, messages]) => {
    if (!Array.isArray(messages) || !messages.every((message) => typeof message === 'string')) {
      return [];
    }

    return [[field, messages] as const];
  });

  return errors.length > 0 ? Object.fromEntries(errors) : undefined;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}
