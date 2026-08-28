import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProblemDetails, toProblemDetails } from '../../core/http/problem-details';

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

@Injectable()
export class ProfileService {
  private readonly http = inject(HttpClient);

  readonly loading = signal(false);
  readonly profile = signal<Profile | null>(null);
  readonly errorStatus = signal<number | null>(null);
  readonly updateLoading = signal(false);
  readonly updateError = signal<ProblemDetails | null>(null);
  readonly passwordLoading = signal(false);
  readonly passwordError = signal<ProblemDetails | null>(null);

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
