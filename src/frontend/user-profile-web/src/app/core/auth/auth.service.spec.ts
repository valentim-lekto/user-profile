import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AUTH_TOKEN_STORAGE_KEY, AuthService } from './auth.service';

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  let router: { url: string; navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    sessionStorage.clear();
    router = { url: '/login', navigate: vi.fn().mockResolvedValue(true) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });

    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    auth.clearSession();
    vi.useRealTimers();
  });

  it('posts the login contract, blocks duplicate requests and stores only the access token', async () => {
    const requestBody = { email: 'ana@example.test', password: 'synthetic-password' };
    const accessToken = createToken(expiresInSeconds(900));

    const firstLogin = auth.login(requestBody);
    const duplicateLogin = auth.login(requestBody);

    expect(auth.loading()).toBe(true);
    expect(auth.error()).toBeNull();
    await expect(duplicateLogin).resolves.toBe(false);

    const request = http.expectOne('/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(requestBody);
    expect(http.match('/api/auth/login')).toHaveLength(0);

    request.flush({ accessToken });

    await expect(firstLogin).resolves.toBe(true);
    expect(auth.loading()).toBe(false);
    expect(auth.hasValidSession()).toBe(true);
    expect(sessionStorage.length).toBe(1);
    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(accessToken);
  });

  it('keeps the generic 401 available to the login screen without creating a session', async () => {
    const result = auth.login({ email: 'missing@example.test', password: 'wrong-password' });

    http.expectOne('/api/auth/login').flush(
      {
        title: 'Unauthorized',
        status: 401,
        detail: 'Invalid email or password.',
      },
      { status: 401, statusText: 'Unauthorized' },
    );

    await expect(result).resolves.toBe(false);
    expect(auth.error()).toEqual({
      title: 'Unauthorized',
      status: 401,
      detail: 'Invalid email or password.',
      errors: undefined,
    });
    expect(sessionStorage.length).toBe(0);
  });

  it('rejects a malformed token returned as a successful response', async () => {
    const result = auth.login({ email: 'ana@example.test', password: 'synthetic-password' });

    http.expectOne('/api/auth/login').flush({ accessToken: 'not-a-jwt' });

    await expect(result).resolves.toBe(false);
    expect(auth.error()).toEqual({ status: 0 });
    expect(sessionStorage.length).toBe(0);
  });

  it('revalidates exp on every session decision and removes a token after it expires', () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-08-26T12:00:00Z'));
    const accessToken = createToken(expiresInSeconds(60));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);

    expect(auth.getValidAccessToken()).toBe(accessToken);

    vi.setSystemTime(new Date('2026-08-26T12:01:01Z'));

    expect(auth.getValidAccessToken()).toBeNull();
    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
  });

  it('expires the current session at exp and redirects an already active protected route', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-08-26T12:00:00Z'));
    router.url = '/dashboard';
    const accessToken = createToken(expiresInSeconds(60));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);

    expect(auth.getValidAccessToken()).toBe(accessToken);

    await vi.advanceTimersByTimeAsync(60_000);

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(router.navigate).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it.each([
    '/dashboard;tab=resumo',
    '/profile;section=password?mode=edit#password-form',
  ])('redirects an active protected route with URL parameters at exp: %s', async (url) => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-08-26T12:00:00Z'));
    router.url = url;
    const accessToken = createToken(expiresInSeconds(60));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
    expect(auth.getValidAccessToken()).toBe(accessToken);

    await vi.advanceTimersByTimeAsync(60_000);

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('does not let an earlier expiration timer clear a newer session', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-08-26T12:00:00Z'));
    router.url = '/profile';
    const firstAccessToken = createToken(expiresInSeconds(60));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, firstAccessToken);
    expect(auth.getValidAccessToken()).toBe(firstAccessToken);

    await vi.advanceTimersByTimeAsync(30_000);
    const newerAccessToken = createToken(expiresInSeconds(300));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, newerAccessToken);
    expect(auth.getValidAccessToken()).toBe(newerAccessToken);

    await vi.advanceTimersByTimeAsync(31_000);

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(newerAccessToken);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('clears an expired session without interrupting an active public route', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-08-26T12:00:00Z'));
    router.url = '/register';
    const accessToken = createToken(expiresInSeconds(60));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
    expect(auth.getValidAccessToken()).toBe(accessToken);

    await vi.advanceTimersByTimeAsync(60_000);

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('cancels the expiration timer when the service is destroyed', async () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date('2026-08-26T12:00:00Z'));
    router.url = '/dashboard';
    const accessToken = createToken(expiresInSeconds(60));
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
    expect(auth.getValidAccessToken()).toBe(accessToken);

    auth.ngOnDestroy();
    await vi.advanceTimersByTimeAsync(60_000);

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(accessToken);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});

function expiresInSeconds(seconds: number): number {
  return Math.floor(Date.now() / 1000) + seconds;
}

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256', typ: 'JWT' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
