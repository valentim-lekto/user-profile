import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  passwordConfirmation: string;
}

export interface MessageResponse {
  message: string;
}

export interface ProblemDetails {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

@Injectable({ providedIn: 'root' })
export class RegistrationService {
  private readonly http = inject(HttpClient);

  readonly loading = signal(false);
  readonly data = signal<MessageResponse | null>(null);
  readonly error = signal<ProblemDetails | null>(null);

  async register(request: RegisterRequest): Promise<MessageResponse | null> {
    if (this.loading()) {
      return null;
    }

    this.loading.set(true);
    this.data.set(null);
    this.error.set(null);

    try {
      const response = await firstValueFrom(
        this.http.post<MessageResponse>('/api/auth/register', request),
      );
      this.data.set(response);
      return response;
    } catch (error: unknown) {
      this.error.set(toProblemDetails(error));
      return null;
    } finally {
      this.loading.set(false);
    }
  }
}

function toProblemDetails(error: unknown): ProblemDetails {
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
