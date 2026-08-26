import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export interface Profile {
  id: string;
  name: string;
  email: string;
}

@Injectable()
export class ProfileService {
  private readonly http = inject(HttpClient);

  readonly loading = signal(false);
  readonly profile = signal<Profile | null>(null);
  readonly errorStatus = signal<number | null>(null);

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
}
