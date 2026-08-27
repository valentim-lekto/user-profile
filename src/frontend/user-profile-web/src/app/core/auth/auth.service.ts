import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, OnDestroy, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';

export const AUTH_TOKEN_STORAGE_KEY = 'user-profile.access-token';

const PROTECTED_ROUTE_PATHS = new Set(['/dashboard', '/profile']);

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
export class AuthService implements OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly accessToken = signal<string | null>(null);
  private expirationTimer: ReturnType<typeof setTimeout> | undefined;

  readonly loading = signal(false);
  readonly error = signal<AuthProblemDetails | null>(null);

  constructor() {
    this.getValidAccessToken();
  }

  ngOnDestroy(): void {
    this.cancelExpirationTimer();
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

      const expiresAt = readUnexpiredJwtExpiration(response.accessToken);
      if (expiresAt === null) {
        this.clearSession();
        this.error.set({ status: 0 });
        return false;
      }

      sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, response.accessToken);
      this.accessToken.set(response.accessToken);
      this.scheduleExpiration(response.accessToken, expiresAt);
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
    const expiresAt = readUnexpiredJwtExpiration(storedToken);

    if (storedToken === null || expiresAt === null) {
      this.clearSession();
      return null;
    }

    if (this.accessToken() !== storedToken) {
      this.accessToken.set(storedToken);
      this.scheduleExpiration(storedToken, expiresAt);
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
    this.cancelExpirationTimer();
    sessionStorage.removeItem(AUTH_TOKEN_STORAGE_KEY);
    this.accessToken.set(null);
  }

  private scheduleExpiration(accessToken: string, expiresAt: number): void {
    this.cancelExpirationTimer();
    this.expirationTimer = setTimeout(() => {
      this.expirationTimer = undefined;

      if (!this.isCurrentAccessToken(accessToken)) {
        return;
      }

      this.clearSession();
      if (PROTECTED_ROUTE_PATHS.has(this.router.url.split(/[;?#]/, 1)[0])) {
        void this.router.navigate(['/login']);
      }
    }, expiresAt - Date.now());
  }

  private cancelExpirationTimer(): void {
    if (this.expirationTimer !== undefined) {
      clearTimeout(this.expirationTimer);
      this.expirationTimer = undefined;
    }
  }
}

function readUnexpiredJwtExpiration(value: unknown): number | null {
  if (typeof value !== 'string') {
    return null;
  }

  const parts = value.split('.');
  if (parts.length !== 3 || parts.some((part) => part.length === 0)) {
    return null;
  }

  try {
    const encodedPayload = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const paddedPayload = encodedPayload.padEnd(Math.ceil(encodedPayload.length / 4) * 4, '=');
    const payload: unknown = JSON.parse(atob(paddedPayload));

    if (!isRecord(payload)) {
      return null;
    }

    const expiresAt = payload['exp'];
    if (typeof expiresAt !== 'number' || !Number.isSafeInteger(expiresAt)) {
      return null;
    }

    const expiresAtMilliseconds = expiresAt * 1000;
    return Number.isSafeInteger(expiresAtMilliseconds) && expiresAtMilliseconds > Date.now()
      ? expiresAtMilliseconds
      : null;
  } catch {
    return null;
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
