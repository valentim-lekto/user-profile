import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import {
  ProblemDetails,
  RegisterRequest,
  RegistrationService,
} from '../registration.service';
import { passwordsMatch, trimmedEmail, trimmedLength } from './register.validators';

type RegisterField = keyof RegisterRequest;
type ApiFieldErrors = Partial<Record<RegisterField, string[]>>;

const API_FIELDS: Record<string, RegisterField> = {
  name: 'name',
  email: 'email',
  password: 'password',
  passwordconfirmation: 'passwordConfirmation',
};

@Component({
  selector: 'app-register',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register.html',
  styleUrl: './register.scss',
})
export class Register {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);

  protected readonly registration = inject(RegistrationService);
  protected readonly apiFieldErrors = signal<ApiFieldErrors>({});
  protected readonly apiError = signal<string | null>(null);

  readonly form = this.formBuilder.group(
    {
      name: ['', [trimmedLength(3, 200)]],
      email: ['', [trimmedLength(1, 320), trimmedEmail]],
      password: ['', [Validators.required, Validators.minLength(6), Validators.maxLength(128)]],
      passwordConfirmation: [
        '',
        [Validators.required, Validators.minLength(6), Validators.maxLength(128)],
      ],
    },
    { validators: passwordsMatch },
  );

  async submit(): Promise<void> {
    if (this.registration.loading()) {
      return;
    }

    this.clearApiErrors();

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const response = await this.registration.register({
      name: value.name.trim(),
      email: value.email.trim(),
      password: value.password,
      passwordConfirmation: value.passwordConfirmation,
    });

    if (response) {
      await this.router.navigate(['/login'], {
        state: { registrationCompleted: true },
      });
      return;
    }

    this.applyApiProblem(this.registration.error());
  }

  protected clearApiFieldError(field: RegisterField): void {
    this.apiError.set(null);

    const currentErrors = this.apiFieldErrors();
    if (currentErrors[field]) {
      const updatedErrors = { ...currentErrors };
      delete updatedErrors[field];
      this.apiFieldErrors.set(updatedErrors);
    }

    const control = this.form.controls[field];
    if (control.hasError('api')) {
      const controlErrors = { ...control.errors };
      delete controlErrors['api'];
      control.setErrors(Object.keys(controlErrors).length > 0 ? controlErrors : null);
    }
  }

  protected fieldError(field: RegisterField): string | null {
    const apiError = this.apiFieldErrors()[field]?.[0];
    if (apiError) {
      return apiError;
    }

    const control = this.form.controls[field];
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return requiredMessage(field);
    }

    if (control.hasError('minlength')) {
      return minimumLengthMessage(field);
    }

    if (control.hasError('maxlength')) {
      return maximumLengthMessage(field);
    }

    if (control.hasError('email')) {
      return 'Informe um email válido.';
    }

    return null;
  }

  private applyApiProblem(problem: ProblemDetails | null): void {
    if (problem?.status === 400) {
      const fieldErrors = mapFieldErrors(problem.errors);
      this.apiFieldErrors.set(fieldErrors);

      for (const field of Object.keys(fieldErrors) as RegisterField[]) {
        const control = this.form.controls[field];
        control.setErrors({ ...control.errors, api: true });
        control.markAsTouched();
      }

      if (Object.keys(fieldErrors).length === 0) {
        this.apiError.set('Revise os dados informados e tente novamente.');
      }

      return;
    }

    if (problem?.status === 409) {
      this.apiError.set('Já existe uma conta cadastrada com este email.');
      return;
    }

    if (problem?.status === 503) {
      this.apiError.set('O serviço está indisponível no momento. Tente novamente em breve.');
      return;
    }

    this.apiError.set('Não foi possível concluir o cadastro. Tente novamente.');
  }

  private clearApiErrors(): void {
    for (const field of Object.keys(API_FIELDS) as string[]) {
      const mappedField = API_FIELDS[field];
      this.clearApiFieldError(mappedField);
    }

    this.apiFieldErrors.set({});
    this.apiError.set(null);
  }
}

function mapFieldErrors(errors: Record<string, string[]> | undefined): ApiFieldErrors {
  if (!errors) {
    return {};
  }

  const mappedErrors: ApiFieldErrors = {};

  for (const [field, messages] of Object.entries(errors)) {
    const mappedField = API_FIELDS[field.toLowerCase()];
    if (mappedField && messages.length > 0) {
      mappedErrors[mappedField] = messages;
    }
  }

  return mappedErrors;
}

function requiredMessage(field: RegisterField): string {
  switch (field) {
    case 'name':
      return 'Informe seu nome.';
    case 'email':
      return 'Informe seu email.';
    case 'password':
      return 'Informe uma senha.';
    case 'passwordConfirmation':
      return 'Confirme sua senha.';
  }
}

function minimumLengthMessage(field: RegisterField): string {
  switch (field) {
    case 'name':
      return 'O nome deve ter pelo menos 3 caracteres.';
    case 'password':
      return 'A senha deve ter pelo menos 6 caracteres.';
    case 'passwordConfirmation':
      return 'A confirmação deve ter pelo menos 6 caracteres.';
    case 'email':
      return 'Informe seu email.';
  }
}

function maximumLengthMessage(field: RegisterField): string {
  switch (field) {
    case 'name':
      return 'O nome deve ter no máximo 200 caracteres.';
    case 'email':
      return 'O email deve ter no máximo 320 caracteres.';
    case 'password':
      return 'A senha deve ter no máximo 128 caracteres.';
    case 'passwordConfirmation':
      return 'A confirmação deve ter no máximo 128 caracteres.';
  }
}
