import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ProblemDetails, toProblemDetails } from '../../core/http/problem-details';

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  passwordConfirmation: string;
}

export interface MessageResponse {
  message: string;
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
