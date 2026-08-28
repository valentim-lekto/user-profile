import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { App } from './app';
import { appConfig } from './app.config';
import { AUTH_TOKEN_STORAGE_KEY } from './core/auth/auth.service';

describe('App', () => {
  beforeEach(async () => {
    sessionStorage.clear();
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [...appConfig.providers, provideHttpClientTesting()],
    }).compileComponents();
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    sessionStorage.clear();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('should render the application shell and router outlet', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('header mat-toolbar')?.textContent).toContain(
      'Perfil do usuário',
    );
    expect(compiled.querySelector<HTMLAnchorElement>('.skip-link')?.getAttribute('href')).toBe(
      '#main-content',
    );
    expect(compiled.querySelector('main')?.getAttribute('tabindex')).toBe('-1');
    expect(compiled.querySelector('router-outlet')).not.toBeNull();
  });

  it(
    'wires the production guard, routes and authentication interceptor',
    async () => {
      const harness = await RouterTestingHarness.create();
      const router = TestBed.inject(Router);
      const http = TestBed.inject(HttpTestingController);

      await harness.navigateByUrl('/dashboard');
      expect(router.url).toBe('/login');

      const accessToken = createToken(Math.floor(Date.now() / 1000) + 900);
      sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
      await harness.navigateByUrl('/dashboard');

      const request = http.expectOne('/api/profile');
      expect(request.request.headers.get('Authorization')).toBe(`Bearer ${accessToken}`);
      request.flush({
        id: '00000000-0000-4000-8000-000000000001',
        name: 'Ana Example',
        email: 'ana@example.test',
      });
      await harness.fixture.whenStable();
      harness.detectChanges();

      expect(harness.routeNativeElement?.textContent).toContain('Boas-vindas, Ana Example!');
    },
  );

  it(
    'wires the real protected profile route to its authenticated load',
    async () => {
      const harness = await RouterTestingHarness.create();
      const router = TestBed.inject(Router);
      const http = TestBed.inject(HttpTestingController);

      await harness.navigateByUrl('/profile');
      expect(router.url).toBe('/login');

      const accessToken = createToken(Math.floor(Date.now() / 1000) + 900);
      sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
      await harness.navigateByUrl('/profile');

      const request = http.expectOne('/api/profile');
      expect(request.request.method).toBe('GET');
      expect(request.request.headers.get('Authorization')).toBe(`Bearer ${accessToken}`);
      request.flush({
        id: '00000000-0000-4000-8000-000000000001',
        name: 'Ana Example',
        email: 'ana@example.test',
      });
      await harness.fixture.whenStable();
      harness.detectChanges();

      expect(harness.routeNativeElement?.textContent).toContain('Dados pessoais');
      expect(harness.routeNativeElement?.textContent).toContain('Alterar senha');
    },
  );
});

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
