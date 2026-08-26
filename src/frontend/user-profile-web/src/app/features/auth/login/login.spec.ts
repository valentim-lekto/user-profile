import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AUTH_TOKEN_STORAGE_KEY } from '../../../core/auth/auth.service';
import { Login } from './login';

@Component({ selector: 'app-dashboard-stub', template: '' })
class DashboardStub {}

@Component({ selector: 'app-origin-stub', template: '' })
class OriginStub {}

describe('Login', () => {
  let component: Login;
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'login', component: Login },
          { path: 'dashboard', component: DashboardStub },
          { path: 'origin', component: OriginStub },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    component = await harness.navigateByUrl('/login', Login);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('validates the trimmed ASCII email and the untrimmed login password limits', () => {
    component.form.markAllAsTouched();
    expect(component.form.controls.email.hasError('required')).toBe(true);
    expect(component.form.controls.password.hasError('required')).toBe(true);

    component.form.controls.email.setValue('  ana@example.test  ');
    component.form.controls.password.setValue(' ');
    expect(component.form.valid).toBe(true);

    component.form.controls.email.setValue('ana@example');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue('ß@example.test');
    expect(component.form.controls.email.hasError('email')).toBe(true);

    component.form.controls.email.setValue(`${'a'.repeat(307)}@example.test`);
    component.form.controls.password.setValue('p'.repeat(128));
    expect(component.form.valid).toBe(true);

    component.form.controls.email.setValue('e'.repeat(321));
    expect(component.form.controls.email.hasError('maxlength')).toBe(true);

    component.form.controls.password.setValue('p'.repeat(129));
    expect(component.form.controls.password.hasError('maxlength')).toBe(true);
  });

  it('shows field errors and does not call the API for a locally invalid form', async () => {
    await component.submit();
    harness.detectChanges();

    http.expectNone('/api/auth/login');
    const errors = Array.from(
      harness.routeNativeElement?.querySelectorAll('mat-error') ?? [],
      (element) => element.textContent?.trim(),
    );
    expect(errors).toContain('Informe seu email.');
    expect(errors).toContain('Informe sua senha.');
  });

  it('shows loading, blocks duplicate submission, stores the token and navigates to dashboard', async () => {
    setValidForm(component);
    const accessToken = createToken(Math.floor(Date.now() / 1000) + 900);

    const submission = component.submit();
    const duplicateSubmission = component.submit();
    const request = http.expectOne('/api/auth/login');
    harness.detectChanges();

    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      email: 'ana@example.test',
      password: 'synthetic-password',
    });
    expect(http.match('/api/auth/login')).toHaveLength(0);
    expect(
      harness.routeNativeElement?.querySelector<HTMLButtonElement>('button[type="submit"]')
        ?.disabled,
    ).toBe(true);
    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Entrando',
    );

    request.flush({ accessToken });
    await Promise.all([submission, duplicateSubmission]);

    expect(router.url).toBe('/dashboard');
    expect(sessionStorage.length).toBe(1);
    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(accessToken);
  });

  it('keeps the generic invalid-credentials error on the login screen', async () => {
    setValidForm(component);

    const submission = component.submit();
    const request = http.expectOne('/api/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(
      {
        title: 'Unauthorized',
        status: 401,
        detail: 'Invalid email or password.',
      },
      { status: 401, statusText: 'Unauthorized' },
    );

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/login');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'Email ou senha inválidos',
    );
    expect(sessionStorage.length).toBe(0);
  });

  it('shows a clear service-unavailable error without navigating', async () => {
    setValidForm(component);

    const submission = component.submit();
    http.expectOne('/api/auth/login').flush(
      { title: 'Service Unavailable', status: 503 },
      { status: 503, statusText: 'Service Unavailable' },
    );

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/login');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'O serviço está indisponível no momento',
    );
  });

  it('keeps a validation 400 on the login screen with a clear message', async () => {
    setValidForm(component);

    const submission = component.submit();
    http.expectOne('/api/auth/login').flush(
      { title: 'Bad Request', status: 400, errors: { email: ['Invalid email.'] } },
      { status: 400, statusText: 'Bad Request' },
    );

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/login');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'Revise os dados informados',
    );
    expect(sessionStorage.length).toBe(0);
  });

  it('keeps an unexpected network error on the login screen with a generic message', async () => {
    setValidForm(component);

    const submission = component.submit();
    http.expectOne('/api/auth/login').error(new ProgressEvent('error'));

    await submission;
    harness.detectChanges();

    expect(router.url).toBe('/login');
    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'Não foi possível entrar',
    );
    expect(sessionStorage.length).toBe(0);
  });

  it('shows the password-change confirmation received through navigation state', async () => {
    await harness.navigateByUrl('/origin', OriginStub);
    await router.navigate(['/login'], { state: { passwordChanged: true } });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Senha alterada com sucesso. Faça login novamente.',
    );
  });
});

function setValidForm(component: Login): void {
  component.form.setValue({
    email: '  ana@example.test  ',
    password: 'synthetic-password',
  });
}

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
