import {
  AbstractControl,
  ValidationErrors,
  ValidatorFn,
} from '@angular/forms';

export const EMAIL_PATTERN = /^[^@\s]+@[^@\s]+\.[^@\s]+$/;

export function trimmedLength(minLength: number, maxLength: number): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value = readTrimmedValue(control);

    if (value.length === 0) {
      return { required: true };
    }

    if (value.length < minLength) {
      return { minlength: { requiredLength: minLength, actualLength: value.length } };
    }

    if (value.length > maxLength) {
      return { maxlength: { requiredLength: maxLength, actualLength: value.length } };
    }

    return null;
  };
}

export const trimmedEmail: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
  const value = readTrimmedValue(control);

  if (value.length === 0) {
    return null;
  }

  return EMAIL_PATTERN.test(value) ? null : { email: true };
};

export const passwordsMatch: ValidatorFn = (
  control: AbstractControl,
): ValidationErrors | null => {
  const password = control.get('password')?.value;
  const passwordConfirmation = control.get('passwordConfirmation')?.value;

  return password === passwordConfirmation ? null : { passwordsMismatch: true };
};

function readTrimmedValue(control: AbstractControl): string {
  return typeof control.value === 'string' ? control.value.trim() : '';
}
