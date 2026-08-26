import { Component, ElementRef, OnInit, inject, signal } from '@angular/core';
import {
  AbstractControl,
  NonNullableFormBuilder,
  ReactiveFormsModule,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { ErrorStateMatcher } from '@angular/material/core';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { trimmedEmail, trimmedLength } from '../auth/register/register.validators';
import {
  ChangePasswordRequest,
  MessageResponse,
  Profile as ProfileResponse,
  ProfileProblemDetails,
  ProfileService,
  UpdateProfileRequest,
} from './profile.service';

type ProfileField = keyof UpdateProfileRequest;
type PasswordField = keyof ChangePasswordRequest;
type ApiFieldErrors<Field extends string> = Partial<Record<Field, string[]>>;

const PROFILE_API_FIELDS: Record<string, ProfileField> = {
  name: 'name',
  email: 'email',
};

const PASSWORD_API_FIELDS: Record<string, PasswordField> = {
  currentpassword: 'currentPassword',
  newpassword: 'newPassword',
  newpasswordconfirmation: 'newPasswordConfirmation',
};

const PROFILE_FIELDS: readonly ProfileField[] = ['name', 'email'];
const PASSWORD_FIELDS: readonly PasswordField[] = [
  'currentPassword',
  'newPassword',
  'newPasswordConfirmation',
];

const newPasswordsMatch: ValidatorFn = (control: AbstractControl) => {
  const password = control.get('newPassword')?.value;
  const confirmation = control.get('newPasswordConfirmation')?.value;

  return password === confirmation ? null : { passwordsMismatch: true };
};

@Component({
  selector: 'app-profile',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  providers: [ProfileService],
  templateUrl: './profile.html',
  styleUrl: './profile.scss',
})
export class Profile implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly element = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly router = inject(Router);

  protected readonly profiles = inject(ProfileService);
  protected readonly profileApiFieldErrors = signal<ApiFieldErrors<ProfileField>>({});
  protected readonly profileError = signal<string | null>(null);
  protected readonly profileSuccess = signal<string | null>(null);
  protected readonly passwordApiFieldErrors = signal<ApiFieldErrors<PasswordField>>({});
  protected readonly passwordError = signal<string | null>(null);

  readonly profileForm = this.formBuilder.group({
    name: ['', [trimmedLength(3, 200)]],
    email: ['', [trimmedLength(1, 320), trimmedEmail]],
  });

  readonly passwordForm = this.formBuilder.group(
    {
      currentPassword: ['', [Validators.required, Validators.maxLength(128)]],
      newPassword: [
        '',
        [Validators.required, Validators.minLength(6), Validators.maxLength(128)],
      ],
      newPasswordConfirmation: [
        '',
        [Validators.required, Validators.minLength(6), Validators.maxLength(128)],
      ],
    },
    { validators: newPasswordsMatch },
  );

  protected readonly newPasswordConfirmationErrorStateMatcher: ErrorStateMatcher = {
    isErrorState: (control, form) =>
      !!control &&
      (control.invalid || this.passwordForm.hasError('passwordsMismatch')) &&
      (control.touched || form?.submitted === true),
  };

  ngOnInit(): void {
    void this.reload();
  }

  async reload(): Promise<void> {
    const profile = await this.profiles.load();

    if (profile) {
      this.profileForm.reset({ name: profile.name, email: profile.email });
    }
  }

  async submitProfile(): Promise<void> {
    if (this.profiles.updateLoading()) {
      return;
    }

    this.clearProfileFeedback();

    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      this.focusFirstInvalidProfileField();
      return;
    }

    const value = this.profileForm.getRawValue();
    let profile: ProfileResponse | null;
    this.profileForm.disable({ emitEvent: false });
    try {
      profile = await this.profiles.update({
        name: value.name.trim(),
        email: value.email.trim(),
      });
    } finally {
      this.profileForm.enable({ emitEvent: false });
    }

    if (profile) {
      this.profileForm.reset({ name: profile.name, email: profile.email });
      this.profileSuccess.set('Dados pessoais atualizados com sucesso.');
      return;
    }

    this.applyProfileProblem(this.profiles.updateError());
  }

  async submitPassword(): Promise<void> {
    if (this.profiles.passwordLoading()) {
      return;
    }

    this.clearPasswordFeedback();

    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      this.focusFirstInvalidPasswordField();
      return;
    }

    const accessToken = this.auth.getValidAccessToken();
    let changed: MessageResponse | null;
    this.passwordForm.disable({ emitEvent: false });
    try {
      changed = await this.profiles.changePassword(this.passwordForm.getRawValue());
    } finally {
      this.passwordForm.enable({ emitEvent: false });
    }

    if (changed) {
      this.passwordForm.reset();
      if (accessToken && this.auth.isCurrentAccessToken(accessToken)) {
        this.auth.clearSession();
        await this.router.navigate(['/login'], { state: { passwordChanged: true } });
      }
      return;
    }

    this.applyPasswordProblem(this.profiles.passwordError());
  }

  protected clearProfileApiFieldError(field: ProfileField): void {
    this.profileError.set(null);
    this.profileSuccess.set(null);
    this.profileApiFieldErrors.update((errors) => withoutField(errors, field));
    clearApiError(this.profileForm.controls[field]);
  }

  protected clearPasswordApiFieldError(field: PasswordField): void {
    this.passwordError.set(null);
    this.passwordApiFieldErrors.update((errors) => withoutField(errors, field));
    clearApiError(this.passwordForm.controls[field]);
  }

  protected profileFieldError(field: ProfileField): string | null {
    if (this.profileApiFieldErrors()[field]) {
      return profileApiValidationMessage(field);
    }

    const control = this.profileForm.controls[field];
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return field === 'name' ? 'Informe seu nome.' : 'Informe seu email.';
    }

    if (control.hasError('minlength')) {
      return 'O nome deve ter pelo menos 3 caracteres.';
    }

    if (control.hasError('maxlength')) {
      return field === 'name'
        ? 'O nome deve ter no máximo 200 caracteres.'
        : 'O email deve ter no máximo 320 caracteres.';
    }

    if (control.hasError('email')) {
      return 'Informe um email válido.';
    }

    return null;
  }

  protected passwordFieldError(field: PasswordField): string | null {
    if (this.passwordApiFieldErrors()[field]) {
      return passwordApiValidationMessage(field);
    }

    const control = this.passwordForm.controls[field];
    if (!control.touched) {
      return null;
    }

    if (control.hasError('required')) {
      return passwordRequiredMessage(field);
    }

    if (control.hasError('minlength')) {
      return field === 'newPassword'
        ? 'A nova senha deve ter pelo menos 6 caracteres.'
        : 'A confirmação deve ter pelo menos 6 caracteres.';
    }

    if (control.hasError('maxlength')) {
      return passwordMaximumLengthMessage(field);
    }

    return null;
  }

  protected newPasswordConfirmationMismatch(): boolean {
    return (
      this.passwordForm.controls.newPasswordConfirmation.touched &&
      this.passwordForm.hasError('passwordsMismatch')
    );
  }

  private applyProfileProblem(problem: ProfileProblemDetails | null): void {
    if (problem?.status === 400) {
      const fieldErrors = mapFieldErrors(problem.errors, PROFILE_API_FIELDS);
      this.profileApiFieldErrors.set(fieldErrors);

      for (const field of Object.keys(fieldErrors) as ProfileField[]) {
        const control = this.profileForm.controls[field];
        control.setErrors({ ...control.errors, api: true });
        control.markAsTouched();
      }

      if (Object.keys(fieldErrors).length === 0) {
        this.profileError.set('Revise os dados informados e tente novamente.');
      } else {
        this.focusFirstProfileApiField(fieldErrors);
      }

      return;
    }

    if (problem?.status === 409) {
      this.profileError.set('Este email já pertence a outra conta.');
      this.focusField('email');
      return;
    }

    if (problem?.status === 503) {
      this.profileError.set('O serviço está indisponível no momento. Tente novamente em breve.');
      return;
    }

    this.profileError.set('Não foi possível atualizar seus dados. Tente novamente.');
  }

  private applyPasswordProblem(problem: ProfileProblemDetails | null): void {
    if (problem?.status === 400) {
      const fieldErrors = mapFieldErrors(problem.errors, PASSWORD_API_FIELDS);
      this.passwordApiFieldErrors.set(fieldErrors);

      for (const field of Object.keys(fieldErrors) as PasswordField[]) {
        const control = this.passwordForm.controls[field];
        control.setErrors({ ...control.errors, api: true });
        control.markAsTouched();
      }

      if (Object.keys(fieldErrors).length === 0) {
        this.passwordError.set('Revise as senhas informadas e tente novamente.');
      } else {
        this.focusFirstPasswordApiField(fieldErrors);
      }

      return;
    }

    if (problem?.status === 503) {
      this.passwordError.set('O serviço está indisponível no momento. Tente novamente em breve.');
      return;
    }

    this.passwordError.set('Não foi possível alterar sua senha. Tente novamente.');
  }

  private clearProfileFeedback(): void {
    this.profileApiFieldErrors.set({});
    this.profileError.set(null);
    this.profileSuccess.set(null);

    for (const field of PROFILE_FIELDS) {
      clearApiError(this.profileForm.controls[field]);
    }
  }

  private clearPasswordFeedback(): void {
    this.passwordApiFieldErrors.set({});
    this.passwordError.set(null);

    for (const field of PASSWORD_FIELDS) {
      clearApiError(this.passwordForm.controls[field]);
    }
  }

  private focusFirstInvalidProfileField(): void {
    const field = PROFILE_FIELDS.find((candidate) => this.profileForm.controls[candidate].invalid);
    if (field) {
      this.focusField(field);
    }
  }

  private focusFirstInvalidPasswordField(): void {
    const field = PASSWORD_FIELDS.find(
      (candidate) => this.passwordForm.controls[candidate].invalid,
    );

    if (field) {
      this.focusField(field);
      return;
    }

    if (this.passwordForm.hasError('passwordsMismatch')) {
      this.focusField('newPasswordConfirmation');
    }
  }

  private focusFirstProfileApiField(errors: ApiFieldErrors<ProfileField>): void {
    const field = PROFILE_FIELDS.find((candidate) => errors[candidate]);
    if (field) {
      this.focusField(field);
    }
  }

  private focusFirstPasswordApiField(errors: ApiFieldErrors<PasswordField>): void {
    const field = PASSWORD_FIELDS.find((candidate) => errors[candidate]);
    if (field) {
      this.focusField(field);
    }
  }

  private focusField(field: ProfileField | PasswordField): void {
    this.element.nativeElement
      .querySelector<HTMLInputElement>(`input[formControlName="${field}"]`)
      ?.focus();
  }
}

function mapFieldErrors<Field extends string>(
  errors: Record<string, string[]> | undefined,
  fields: Record<string, Field>,
): ApiFieldErrors<Field> {
  if (!errors) {
    return {};
  }

  const mappedErrors: ApiFieldErrors<Field> = {};

  for (const [field, messages] of Object.entries(errors)) {
    const mappedField = fields[field.toLowerCase()];
    if (mappedField && messages.length > 0) {
      mappedErrors[mappedField] = messages;
    }
  }

  return mappedErrors;
}

function withoutField<Field extends string>(
  errors: ApiFieldErrors<Field>,
  field: Field,
): ApiFieldErrors<Field> {
  if (!errors[field]) {
    return errors;
  }

  const updatedErrors = { ...errors };
  delete updatedErrors[field];
  return updatedErrors;
}

function clearApiError(control: AbstractControl): void {
  if (!control.hasError('api')) {
    return;
  }

  const errors = { ...control.errors };
  delete errors['api'];
  control.setErrors(Object.keys(errors).length > 0 ? errors : null);
}

function passwordRequiredMessage(field: PasswordField): string {
  switch (field) {
    case 'currentPassword':
      return 'Informe sua senha atual.';
    case 'newPassword':
      return 'Informe a nova senha.';
    case 'newPasswordConfirmation':
      return 'Confirme a nova senha.';
  }
}

function passwordMaximumLengthMessage(field: PasswordField): string {
  switch (field) {
    case 'currentPassword':
      return 'A senha atual deve ter no máximo 128 caracteres.';
    case 'newPassword':
      return 'A nova senha deve ter no máximo 128 caracteres.';
    case 'newPasswordConfirmation':
      return 'A confirmação deve ter no máximo 128 caracteres.';
  }
}

function profileApiValidationMessage(field: ProfileField): string {
  return field === 'name' ? 'Revise o nome informado.' : 'Revise o email informado.';
}

function passwordApiValidationMessage(field: PasswordField): string {
  switch (field) {
    case 'currentPassword':
      return 'A senha atual está incorreta.';
    case 'newPassword':
      return 'Revise a nova senha informada.';
    case 'newPasswordConfirmation':
      return 'Revise a confirmação da nova senha.';
  }
}
