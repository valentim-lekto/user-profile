import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Profile {
  id: string;
  name: string;
  email: string;
}

export interface UpdateProfileRequest {
  name: string;
  email: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  newPasswordConfirmation: string;
}

export interface MessageResponse {
  message: string;
}

export interface ProfileProblemDetails {
  status: number;
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

@Injectable()
export class ProfileService {
  private readonly http = inject(HttpClient);

  readonly loading = signal(false);
  readonly profile = signal<Profile | null>(null);
  readonly errorStatus = signal<number | null>(null);
  readonly updateLoading = signal(false);
  readonly updateError = signal<ProfileProblemDetails | null>(null);
  readonly passwordLoading = signal(false);
  readonly passwordError = signal<ProfileProblemDetails | null>(null);

  async load(): Promise<Profile | null> {
    if (this.loading()) {
      return null;
    }

    this.loading.set(true);
    this.profile.set(null);
    this.errorStatus.set(null);

    try {
      const profile = await firstValueFrom(this.http.get<Profile>('/api/profile'));
      this.profile.set(profile);
      return profile;
    } catch (error: unknown) {
      this.errorStatus.set(error instanceof HttpErrorResponse ? error.status : 0);
      return null;
    } finally {
      this.loading.set(false);
    }
  }

  async update(request: UpdateProfileRequest): Promise<Profile | null> {
    if (this.updateLoading()) {
      return null;
    }

    this.updateLoading.set(true);
    this.updateError.set(null);

    try {
      const profile = await firstValueFrom(this.http.put<Profile>('/api/profile', request));
      this.profile.set(profile);
      return profile;
    } catch (error: unknown) {
      this.updateError.set(toProblemDetails(error));
      return null;
    } finally {
      this.updateLoading.set(false);
    }
  }

  async changePassword(request: ChangePasswordRequest): Promise<MessageResponse | null> {
    if (this.passwordLoading()) {
      return null;
    }

    this.passwordLoading.set(true);
    this.passwordError.set(null);

    try {
      return await firstValueFrom(
        this.http.put<MessageResponse>('/api/profile/password', request),
      );
    } catch (error: unknown) {
      this.passwordError.set(toProblemDetails(error));
      return null;
    } finally {
      this.passwordLoading.set(false);
    }
  }
}

function toProblemDetails(error: unknown): ProfileProblemDetails {
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
