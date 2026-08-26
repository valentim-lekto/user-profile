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

    component.form.controls.email.setValue('e'.repeat(321));
    expect(component.form.controls.email.hasError('maxlength')).toBe(true);
  });

  it('validates password limits and exact confirmation', () => {
    component.form.controls.password.setValue('short');
    component.form.controls.passwordConfirmation.setValue('short');
    expect(component.form.controls.password.hasError('minlength')).toBe(true);
    expect(component.form.controls.passwordConfirmation.hasError('minlength')).toBe(true);

    component.form.controls.password.setValue('valid-password');
    component.form.controls.passwordConfirmation.setValue('different-password');
    expect(component.form.hasError('passwordsMismatch')).toBe(true);

    component.form.controls.passwordConfirmation.setValue('valid-password');
    expect(component.form.hasError('passwordsMismatch')).toBe(false);

    component.form.controls.password.setValue('p'.repeat(129));
    component.form.controls.passwordConfirmation.setValue('p'.repeat(129));
    expect(component.form.controls.password.hasError('maxlength')).toBe(true);
    expect(component.form.controls.passwordConfirmation.hasError('maxlength')).toBe(true);
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
        errors: { email: ['Este email não pode ser utilizado.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/register');
    expect(harness.routeNativeElement?.textContent).toContain('Este email não pode ser utilizado.');
    expect(component.form.controls.email.hasError('api')).toBe(true);
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
