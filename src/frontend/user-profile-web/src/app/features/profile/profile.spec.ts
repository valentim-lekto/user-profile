import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { Login } from '../auth/login/login';
import { AUTH_TOKEN_STORAGE_KEY } from '../../core/auth/auth.service';
import { Profile } from './profile';

const CURRENT_PROFILE = {
  id: '00000000-0000-4000-8000-000000000001',
  name: 'Ana Example',
  email: 'ana@example.test',
};

@Component({ template: '' })
class DashboardStub {}

describe('Profile', () => {
  let component: Profile;
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;
  let accessToken: string;

  beforeEach(async () => {
    sessionStorage.clear();
    accessToken = createToken(Math.floor(Date.now() / 1000) + 900);
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);

    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'profile', component: Profile },
          { path: 'dashboard', component: DashboardStub },
          { path: 'login', component: Login },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    component = await harness.navigateByUrl('/profile', Profile);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('loads the current profile into the form and displays its immutable id', async () => {
    harness.detectChanges();
    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Carregando seu perfil',
    );

    await flushProfile();

    expect(component.profileForm.getRawValue()).toEqual({
      name: CURRENT_PROFILE.name,
      email: CURRENT_PROFILE.email,
    });
    expect(harness.routeNativeElement?.textContent).toContain(CURRENT_PROFILE.id);
    expect(harness.routeNativeElement?.textContent).toContain('Dados pessoais');
    expect(harness.routeNativeElement?.textContent).toContain('Alterar senha');
  });

  it('shows a load error and retries the real profile request', async () => {
    http.expectOne('/api/profile').flush(
      { title: 'Service Unavailable', status: 503 },
      { status: 503, statusText: 'Service Unavailable' },
    );
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'O serviço está indisponível no momento',
    );

    const retry = Array.from(
      harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('button') ?? [],
    ).find((button) => button.textContent?.includes('Tentar novamente'));
    retry?.click();

    const request = http.expectOne('/api/profile');
    expect(request.request.method).toBe('GET');
    request.flush(CURRENT_PROFILE);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(component.profileForm.controls.email.value).toBe(CURRENT_PROFILE.email);
  });

  it('applies the registration rules to profile data and the password contract limits', async () => {
    await flushProfile();

    component.profileForm.controls.name.setValue('  ab  ');
    expect(component.profileForm.controls.name.hasError('minlength')).toBe(true);
    component.profileForm.controls.name.setValue(`  ${'n'.repeat(200)}  `);
    expect(component.profileForm.controls.name.valid).toBe(true);
    component.profileForm.controls.name.setValue('n'.repeat(201));
    expect(component.profileForm.controls.name.hasError('maxlength')).toBe(true);

    component.profileForm.controls.email.setValue('ana@example');
    expect(component.profileForm.controls.email.hasError('email')).toBe(true);
    component.profileForm.controls.email.setValue('ß@example.test');
    expect(component.profileForm.controls.email.hasError('email')).toBe(true);
    component.profileForm.controls.email.setValue(`${'a'.repeat(307)}@example.test`);
    expect(component.profileForm.controls.email.valid).toBe(true);
    component.profileForm.controls.email.setValue('e'.repeat(321));
    expect(component.profileForm.controls.email.hasError('maxlength')).toBe(true);

    component.passwordForm.markAllAsTouched();
    expect(component.passwordForm.controls.currentPassword.hasError('required')).toBe(true);
    expect(component.passwordForm.controls.newPassword.hasError('required')).toBe(true);
    expect(component.passwordForm.controls.newPasswordConfirmation.hasError('required')).toBe(
      true,
    );

    component.passwordForm.setValue({
      currentPassword: ' ',
      newPassword: '      ',
      newPasswordConfirmation: '      ',
    });
    expect(component.passwordForm.valid).toBe(true);

    component.passwordForm.controls.newPasswordConfirmation.setValue('different');
    expect(component.passwordForm.hasError('passwordsMismatch')).toBe(true);
    harness.detectChanges();
    expect(
      harness.routeNativeElement?.querySelector('.field-error[role="alert"]')?.textContent,
    ).toContain('A confirmação deve ser idêntica à nova senha.');

    component.passwordForm.setValue({
      currentPassword: 'p'.repeat(129),
      newPassword: 'n'.repeat(129),
      newPasswordConfirmation: 'n'.repeat(129),
    });
    expect(component.passwordForm.controls.currentPassword.hasError('maxlength')).toBe(true);
    expect(component.passwordForm.controls.newPassword.hasError('maxlength')).toBe(true);
    expect(component.passwordForm.controls.newPasswordConfirmation.hasError('maxlength')).toBe(
      true,
    );

    await component.submitProfile();
    await component.submitPassword();
    http.expectNone((request) => request.method === 'PUT');
  });

  it('sends only trimmed name and email, shows loading and blocks duplicate updates', async () => {
    await flushProfile();
    component.profileForm.setValue({
      name: '  Ana Updated  ',
      email: '  ana.updated@example.test  ',
    });

    const firstSubmission = component.submitProfile();
    const duplicateSubmission = component.submitProfile();
    const request = http.expectOne('/api/profile');
    harness.detectChanges();

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      name: 'Ana Updated',
      email: 'ana.updated@example.test',
    });
    expect(Object.keys(request.request.body as object)).toEqual(['name', 'email']);
    expect(http.match((candidate) => candidate.method === 'PUT')).toHaveLength(0);
    expect(
      harness.routeNativeElement?.querySelector<HTMLButtonElement>(
        '[data-testid="save-profile"]',
      )?.disabled,
    ).toBe(true);
    expect(harness.routeNativeElement?.textContent).toContain('Salvando dados pessoais');

    request.flush({
      ...CURRENT_PROFILE,
      name: 'Ana Updated',
      email: 'ana.updated@example.test',
    });
    await Promise.all([firstSubmission, duplicateSubmission]);
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain(
      'Dados pessoais atualizados com sucesso',
    );
    expect(component.profileForm.getRawValue()).toEqual({
      name: 'Ana Updated',
      email: 'ana.updated@example.test',
    });
  });

  it('maps profile validation errors to fields and reports duplicate email conflicts', async () => {
    await flushProfile();
    component.profileForm.setValue({
      name: 'Ana Example',
      email: 'conflict@example.test',
    });

    const invalidSubmission = component.submitProfile();
    http.expectOne('/api/profile').flush(
      {
        title: 'Bad Request',
        status: 400,
        errors: { Email: ['O email informado não é válido.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await invalidSubmission;
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('mat-error')?.textContent).toContain(
      'O email informado não é válido',
    );
    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(accessToken);

    const emailInput = harness.routeNativeElement?.querySelector<HTMLInputElement>(
      'input[formControlName="email"]',
    );
    if (emailInput) {
      emailInput.value = 'other@example.test';
      emailInput.dispatchEvent(new Event('input'));
    }
    await harness.fixture.whenStable();

    const conflictSubmission = component.submitProfile();
    http.expectOne('/api/profile').flush(
      { title: 'Conflict', status: 409 },
      { status: 409, statusText: 'Conflict' },
    );
    await conflictSubmission;
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'Este email já pertence a outra conta',
    );
    expect(router.url).toBe('/profile');
  });

  it('keeps the session and maps the error when the current password is wrong', async () => {
    await flushProfile();
    setValidPasswordForm();

    const submission = component.submitPassword();
    const request = http.expectOne('/api/profile/password');
    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      currentPassword: 'synthetic-current-password',
      newPassword: 'synthetic-new-password',
      newPasswordConfirmation: 'synthetic-new-password',
    });
    request.flush(
      {
        title: 'Bad Request',
        status: 400,
        errors: { CurrentPassword: ['A senha atual está incorreta.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    await submission;
    harness.detectChanges();

    const errors = Array.from(
      harness.routeNativeElement?.querySelectorAll('mat-error') ?? [],
      (element) => element.textContent,
    );
    expect(errors.some((error) => error?.includes('A senha atual está incorreta'))).toBe(true);
    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(accessToken);
    expect(router.url).toBe('/profile');
  });

  it('blocks duplicate password changes, clears only the app session and reports success at login', async () => {
    await flushProfile();
    sessionStorage.setItem('unrelated.test-key', 'preserve-me');
    setValidPasswordForm();

    const firstSubmission = component.submitPassword();
    const duplicateSubmission = component.submitPassword();
    const request = http.expectOne('/api/profile/password');
    harness.detectChanges();

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual({
      currentPassword: 'synthetic-current-password',
      newPassword: 'synthetic-new-password',
      newPasswordConfirmation: 'synthetic-new-password',
    });
    expect(http.match('/api/profile/password')).toHaveLength(0);
    expect(
      harness.routeNativeElement?.querySelector<HTMLButtonElement>(
        '[data-testid="change-password"]',
      )?.disabled,
    ).toBe(true);
    expect(harness.routeNativeElement?.textContent).toContain('Alterando senha');

    request.flush({ message: 'Password changed successfully.' });
    await Promise.all([firstSubmission, duplicateSubmission]);
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(sessionStorage.getItem('unrelated.test-key')).toBe('preserve-me');
    expect(router.url).toBe('/login');
    expect(harness.routeNativeElement?.textContent).toContain(
      'Senha alterada com sucesso. Faça login novamente.',
    );
  });

  async function flushProfile(): Promise<void> {
    const request = http.expectOne('/api/profile');
    expect(request.request.method).toBe('GET');
    request.flush(CURRENT_PROFILE);
    await harness.fixture.whenStable();
    harness.detectChanges();
  }

  function setValidPasswordForm(): void {
    component.passwordForm.setValue({
      currentPassword: 'synthetic-current-password',
      newPassword: 'synthetic-new-password',
      newPasswordConfirmation: 'synthetic-new-password',
    });
  }
});

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
