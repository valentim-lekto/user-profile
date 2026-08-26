import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { routes } from '../../../app.routes';
import { Register } from './register';

describe('Register', () => {
  let component: Register;
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    window.sessionStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideRouter(routes),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    component = await harness.navigateByUrl('/register', Register);
  });

  afterEach(() => {
    http.verify();
    window.sessionStorage.clear();
  });

  it('validates required, trimmed name and trimmed email rules', () => {
    component.form.markAllAsTouched();
    expect(component.form.controls.name.hasError('required')).toBe(true);
    expect(component.form.controls.email.hasError('required')).toBe(true);

    component.form.controls.name.setValue('  ab  ');
    expect(component.form.controls.name.hasError('minlength')).toBe(true);

    component.form.controls.name.setValue('Ana');
    expect(component.form.controls.name.valid).toBe(true);

    component.form.controls.name.setValue(`  ${'n'.repeat(200)}  `);
    expect(component.form.controls.name.valid).toBe(true);

    component.form.controls.name.setValue('n'.repeat(201));
    expect(component.form.controls.name.hasError('maxlength')).toBe(true);

    component.form.controls.email.setValue('  ana@example.test  ');
    expect(component.form.controls.email.valid).toBe(true);

    component.form.controls.email.setValue(`${'a'.repeat(307)}@example.test`);
    expect(component.form.controls.email.value.length).toBe(320);
    expect(component.form.controls.email.valid).toBe(true);

    component.form.controls.email.setValue('not-an-email');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue('ana@example');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue('ana @example.test');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue('ß@example.test');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue('ẞ@example.test');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue('e'.repeat(321));
    expect(component.form.controls.email.hasError('maxlength')).toBe(true);
  });

  it('validates password limits and exact confirmation', () => {
    component.form.controls.password.setValue('short');
    component.form.controls.passwordConfirmation.setValue('short');
    expect(component.form.controls.password.hasError('minlength')).toBe(true);
    expect(component.form.controls.passwordConfirmation.hasError('minlength')).toBe(true);

    component.form.controls.password.setValue(' '.repeat(6));
    component.form.controls.passwordConfirmation.setValue(' '.repeat(6));
    expect(component.form.controls.password.valid).toBe(true);
    expect(component.form.controls.passwordConfirmation.valid).toBe(true);
    expect(component.form.hasError('passwordsMismatch')).toBe(false);

    component.form.controls.password.setValue('valid-password');
    component.form.controls.passwordConfirmation.setValue('different-password');
    component.form.controls.passwordConfirmation.markAsTouched();
    expect(component.form.hasError('passwordsMismatch')).toBe(true);
    harness.detectChanges();
    const confirmation = harness.routeNativeElement?.querySelector<HTMLInputElement>(
      'input[formControlName="passwordConfirmation"]',
    );
    expect(confirmation?.getAttribute('aria-invalid')).toBe('true');
    expect(confirmation?.getAttribute('aria-errormessage')).toBe(
      'register-password-mismatch',
    );

    component.form.controls.passwordConfirmation.setValue('valid-password');
    expect(component.form.hasError('passwordsMismatch')).toBe(false);
    harness.detectChanges();
    expect(confirmation?.getAttribute('aria-invalid')).toBe('false');
    expect(confirmation?.hasAttribute('aria-errormessage')).toBe(false);

    component.form.controls.password.setValue('p'.repeat(128));
    component.form.controls.passwordConfirmation.setValue('p'.repeat(128));
    expect(component.form.controls.password.valid).toBe(true);
    expect(component.form.controls.passwordConfirmation.valid).toBe(true);
    expect(component.form.hasError('passwordsMismatch')).toBe(false);

    component.form.controls.password.setValue('p'.repeat(129));
    component.form.controls.passwordConfirmation.setValue('p'.repeat(129));
    expect(component.form.controls.password.hasError('maxlength')).toBe(true);
    expect(component.form.controls.passwordConfirmation.hasError('maxlength')).toBe(true);
  });

  it('shows local validation messages and does not submit an invalid form', () => {
    component.form.setValue({
      name: 'ab',
      email: 'ana@example',
      password: 'short',
      passwordConfirmation: 'different',
    });
    harness.detectChanges();

    const form = harness.routeNativeElement?.querySelector<HTMLFormElement>('form');
    expect(form).not.toBeNull();
    form?.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
    harness.detectChanges();

    const pageText = harness.routeNativeElement?.textContent ?? '';
    expect(pageText).toContain('O nome deve ter pelo menos 3 caracteres.');
    expect(pageText).toContain('Informe um email válido.');
    expect(pageText).toContain('A senha deve ter pelo menos 6 caracteres.');
    expect(pageText).toContain('A confirmação deve ser idêntica à senha.');
    const headings = harness.routeNativeElement?.querySelectorAll('h1') ?? [];
    expect(headings).toHaveLength(1);
    expect(headings[0]?.textContent).toContain('Criar conta');
    expect(document.activeElement?.getAttribute('formControlName')).toBe('name');

    const confirmation = harness.routeNativeElement?.querySelector<HTMLInputElement>(
      'input[formControlName="passwordConfirmation"]',
    );
    expect(confirmation?.getAttribute('aria-invalid')).toBe('true');
    expect(confirmation?.getAttribute('aria-errormessage')).toBe(
      'register-password-mismatch',
    );
    expect(
      harness.routeNativeElement?.querySelector('#register-password-mismatch')?.textContent,
    ).toContain('A confirmação deve ser idêntica à senha.');
    expect(router.url).toBe('/register');
    http.expectNone('/api/auth/register');
  });

  it('shows loading, blocks duplicate submission and redirects to login with success', async () => {
    setValidFormValues(component);

    const submission = component.submit();
    const request = http.expectOne('/api/auth/register');
    harness.detectChanges();

    expect(request.request.body).toEqual({
      name: 'Ana Example',
      email: 'ana@example.test',
      password: 'synthetic-password',
      passwordConfirmation: 'synthetic-password',
    });

    const submitButton = harness.routeNativeElement?.querySelector<HTMLButtonElement>(
      'button[type="submit"]',
    );
    expect(submitButton?.disabled).toBe(true);
    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Enviando cadastro',
    );

    const duplicateSubmission = component.submit();
    http.expectNone('/api/auth/register');

    request.flush(
      { message: 'Registration completed successfully.' },
      { status: 201, statusText: 'Created' },
    );

    await Promise.all([submission, duplicateSubmission]);
    harness.detectChanges();

    expect(router.url).toBe('/login');
    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Cadastro realizado com sucesso',
    );
    expect(window.sessionStorage.length).toBe(0);
  });

  it('shows validation errors returned by the API and stays on register', async () => {
    setValidFormValues(component);

    const submission = component.submit();
    http.expectOne('/api/auth/register').flush(
      {
        title: 'Bad Request',
        status: 400,
        errors: { email: ['Email must be valid.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/register');
    expect(harness.routeNativeElement?.textContent).toContain('Revise o email informado.');
    expect(harness.routeNativeElement?.textContent).not.toContain('Email must be valid.');
    expect(component.form.controls.email.hasError('api')).toBe(true);
    expect(document.activeElement?.getAttribute('formControlName')).toBe('email');
  });

  it('shows a clear conflict error and allows a corrected retry', async () => {
    setValidFormValues(component);

    const firstSubmission = component.submit();
    http.expectOne('/api/auth/register').flush(
      { title: 'Conflict', status: 409, detail: 'An account with this email already exists.' },
      { status: 409, statusText: 'Conflict' },
    );

    await firstSubmission;
    harness.detectChanges();

    expect(router.url).toBe('/register');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'Já existe uma conta cadastrada com este email',
    );
    expect(document.activeElement?.getAttribute('formControlName')).toBe('email');

    component.form.controls.email.setValue('another@example.test');
    const retry = component.submit();
    const request = http.expectOne('/api/auth/register');
    request.flush(
      { message: 'Registration completed successfully.' },
      { status: 201, statusText: 'Created' },
    );
    await retry;
  });

  it('shows a safe message when the API is unavailable', async () => {
    setValidFormValues(component);

    const submission = component.submit();
    http.expectOne('/api/auth/register').flush(
      {
        title: 'Service Unavailable',
        status: 503,
        detail: 'The service is not ready.',
      },
      { status: 503, statusText: 'Service Unavailable' },
    );

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/register');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'O serviço está indisponível no momento',
    );
  });
});

function setValidFormValues(component: Register): void {
  component.form.setValue({
    name: '  Ana Example  ',
    email: '  ana@example.test  ',
    password: 'synthetic-password',
    passwordConfirmation: 'synthetic-password',
  });
}
