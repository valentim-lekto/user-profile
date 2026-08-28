import { Component } from '@angular/core';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { AUTH_TOKEN_STORAGE_KEY } from '../../core/auth/auth.service';
import { Dashboard } from './dashboard';

@Component({ selector: 'app-login-stub', template: '' })
class LoginStub {}

@Component({ selector: 'app-profile-stub', template: '' })
class ProfileStub {}

describe('Dashboard', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    sessionStorage.clear();
    sessionStorage.setItem(
      AUTH_TOKEN_STORAGE_KEY,
      createToken(Math.floor(Date.now() / 1000) + 900),
    );
    TestBed.configureTestingModule({
      providers: [
        provideRouter([
          { path: 'dashboard', component: Dashboard },
          { path: 'login', component: LoginStub },
          { path: 'profile', component: ProfileStub },
        ]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
    await harness.navigateByUrl('/dashboard', Dashboard);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('shows loading, then welcomes the user by the API name and offers profile navigation', async () => {
    harness.detectChanges();
    const headings = harness.routeNativeElement?.querySelectorAll('h1') ?? [];
    expect(headings).toHaveLength(1);
    expect(headings[0]?.textContent).toContain('Início');
    expect(harness.routeNativeElement?.querySelector('[role="status"]')?.textContent).toContain(
      'Carregando seu perfil',
    );

    http.expectOne('/api/profile').flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: 'Ana Example',
      email: 'ana@example.test',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('Boas-vindas, Ana Example!');
    expect(harness.routeNativeElement?.querySelector('h2.welcome-title')).not.toBeNull();
    expect(harness.routeNativeElement?.textContent).toContain(
      'Consulte ou atualize seus dados no perfil.',
    );
    expect(harness.routeNativeElement?.querySelector('.profile-preview')?.textContent).toContain(
      'ana@example.test',
    );
    const profileLink = harness.routeNativeElement?.querySelector<HTMLAnchorElement>(
      'a[href="/profile"]',
    );
    expect(profileLink).not.toBeNull();

    profileLink?.click();
    await harness.fixture.whenStable();
    expect(router.url).toBe('/profile');
  });

  it('keeps the dashboard focused on required actions without redundant explanatory cards', async () => {
    http.expectOne('/api/profile').flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: 'Ana Example',
      email: 'ana@example.test',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    const content = harness.routeNativeElement?.textContent ?? '';
    expect(content).not.toContain('Mantenha seu nome e email sempre atualizados.');
    expect(content).not.toContain('Altere sua senha com confirmação e feedback claros.');
    expect(content).not.toContain('Encerre seu acesso com segurança quando terminar.');
  });

  it('wraps a defensively long name without losing the welcome content', async () => {
    const longName = 'N'.repeat(200);
    http.expectOne('/api/profile').flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: longName,
      email: 'ana@example.test',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    const welcome = harness.routeNativeElement?.querySelector<HTMLElement>('.welcome-title');
    expect(welcome?.textContent).toContain(longName);
    expect(welcome ? getComputedStyle(welcome).overflowWrap : null).toBe('anywhere');
  });

  it('shows a clear error when the profile cannot be loaded', async () => {
    http.expectOne('/api/profile').flush(
      { title: 'Service Unavailable', status: 503 },
      { status: 503, statusText: 'Service Unavailable' },
    );
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain(
      'O serviço está indisponível no momento',
    );
    expect(harness.routeNativeElement?.querySelectorAll('h1')).toHaveLength(1);
  });

  it('removes the token and returns to login on logout', async () => {
    sessionStorage.setItem('unrelated.test-key', 'preserve-me');
    http.expectOne('/api/profile').flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: 'Ana Example',
      email: 'ana@example.test',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    const logoutButton = Array.from(
      harness.routeNativeElement?.querySelectorAll<HTMLButtonElement>('button') ?? [],
    ).find((button) => button.textContent?.includes('Sair'));
    logoutButton?.click();
    await harness.fixture.whenStable();

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(sessionStorage.getItem('unrelated.test-key')).toBe('preserve-me');
    expect(router.url).toBe('/login');
  });

  it('shows the updated name after returning from profile and consulting the API again', async () => {
    http.expectOne('/api/profile').flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: 'Ana Example',
      email: 'ana@example.test',
    });
    await harness.fixture.whenStable();

    await harness.navigateByUrl('/profile', ProfileStub);
    await harness.navigateByUrl('/dashboard', Dashboard);

    http.expectOne('/api/profile').flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: 'Ana Updated',
      email: 'ana.updated@example.test',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('Boas-vindas, Ana Updated!');
    expect(harness.routeNativeElement?.textContent).not.toContain('Boas-vindas, Ana Example!');
  });

  it('isolates a pending profile response from the next authenticated session', async () => {
    const firstRequest = http.expectOne('/api/profile');

    await router.navigateByUrl('/login');
    sessionStorage.setItem(
      AUTH_TOKEN_STORAGE_KEY,
      createToken(Math.floor(Date.now() / 1000) + 1800),
    );
    await harness.navigateByUrl('/dashboard', Dashboard);

    const secondRequest = http.expectOne('/api/profile');
    firstRequest.flush({
      id: '00000000-0000-4000-8000-000000000001',
      name: 'Sessão anterior',
      email: 'old-session@example.test',
    });
    secondRequest.flush({
      id: '00000000-0000-4000-8000-000000000002',
      name: 'Sessão atual',
      email: 'current-session@example.test',
    });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(harness.routeNativeElement?.textContent).toContain('Boas-vindas, Sessão atual!');
    expect(harness.routeNativeElement?.textContent).not.toContain('Sessão anterior');
  });
});

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
