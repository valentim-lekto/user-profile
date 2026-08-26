import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { trimmedEmail, trimmedLength } from '../register/register.validators';

type LoginField = 'email' | 'password';

@Component({
  selector: 'app-login',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './login.html',
  styleUrl: './login.scss',
})
export class Login {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);

  protected readonly auth = inject(AuthService);
  protected readonly apiError = signal<string | null>(null);
  protected readonly registrationCompleted =
    this.router.currentNavigation()?.extras.state?.['registrationCompleted'] === true;

  readonly form = this.formBuilder.group({
    email: ['', [trimmedLength(1, 320), trimmedEmail]],
    password: ['', [Validators.required, Validators.maxLength(128)]],
  });

  async submit(): Promise<void> {
    if (this.auth.loading()) {
      return;
    }

    this.apiError.set(null);

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const authenticated = await this.auth.login({
      email: value.email.trim(),
      password: value.password,
    });

    if (authenticated) {
      await this.router.navigate(['/dashboard']);
      return;
    }

    this.apiError.set(loginErrorMessage(this.auth.error()?.status));
  }

  protected clearApiError(): void {
    this.apiError.set(null);
  }

  protected fieldError(field: LoginField): string | null {
    const control = this.form.controls[field];
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return field === 'email' ? 'Informe seu email.' : 'Informe sua senha.';
    }

    if (control.hasError('maxlength')) {
      return field === 'email'
        ? 'O email deve ter no máximo 320 caracteres.'
        : 'A senha deve ter no máximo 128 caracteres.';
    }

    if (control.hasError('email')) {
      return 'Informe um email válido.';
    }

    return null;
  }
}

function loginErrorMessage(status: number | undefined): string {
  if (status === 401) {
    return 'Email ou senha inválidos.';
  }

  if (status === 400) {
    return 'Revise os dados informados e tente novamente.';
  }

  if (status === 503) {
    return 'O serviço está indisponível no momento. Tente novamente em breve.';
  }

  return 'Não foi possível entrar. Tente novamente.';
}
