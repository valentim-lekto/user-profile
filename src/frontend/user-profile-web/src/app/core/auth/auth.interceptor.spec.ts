import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { AUTH_TOKEN_STORAGE_KEY } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  let client: HttpClient;
  let http: HttpTestingController;
  let router: { navigate: ReturnType<typeof vi.fn> };
  let accessToken: string;

  beforeEach(() => {
    sessionStorage.clear();
    accessToken = createToken(Math.floor(Date.now() / 1000) + 900);
    sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
    router = { navigate: vi.fn().mockResolvedValue(true) };

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: Router, useValue: router },
      ],
    });

    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    sessionStorage.clear();
  });

  it('adds Bearer only to the exact relative protected method and URL pairs', () => {
    const requests = [
      client.get('/api/profile'),
      client.put('/api/profile', {}),
      client.put('/api/profile/password', {}),
    ];

    for (const request$ of requests) {
      request$.subscribe();
    }

    const expectedRequests = [
      ...http.match('/api/profile'),
      http.expectOne('/api/profile/password'),
    ];

    expect(expectedRequests).toHaveLength(3);
    for (const request of expectedRequests) {
      expect(request.request.headers.get('Authorization')).toBe(`Bearer ${accessToken}`);
      request.flush({});
    }
  });

  it.each([
    ['POST', '/api/auth/login'],
    ['POST', '/api/auth/register'],
    ['GET', '/health'],
    ['GET', 'http://localhost:8080/api/profile'],
    ['GET', 'https://external.example/api/profile'],
    ['POST', '/api/profile'],
    ['GET', '/api/profile?userId=other'],
  ])('does not add Bearer to public, absolute or non-allowlisted %s %s', (method, url) => {
    client.request(method, url).subscribe();

    const request = http.expectOne(url);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('does not add Bearer when an allowlisted path receives query parameters', () => {
    client.get('/api/profile', { params: { userId: 'other' } }).subscribe();

    const request = http.expectOne('/api/profile?userId=other');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('clears the session and navigates to login when an attached Bearer receives 401', async () => {
    sessionStorage.setItem('unrelated.test-key', 'preserve-me');
    const result = firstValueFrom(client.get('/api/profile')).catch(() => null);

    http.expectOne('/api/profile').flush(
      { title: 'Unauthorized', status: 401 },
      { status: 401, statusText: 'Unauthorized' },
    );

    await result;

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
    expect(sessionStorage.getItem('unrelated.test-key')).toBe('preserve-me');
    expect(router.navigate).toHaveBeenCalledOnce();
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('leaves a public login 401 for the screen and keeps the existing session untouched', async () => {
    const result = firstValueFrom(client.post('/api/auth/login', {})).catch(() => null);

    const request = http.expectOne('/api/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(
      { title: 'Unauthorized', status: 401 },
      { status: 401, statusText: 'Unauthorized' },
    );

    await result;

    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBe(accessToken);
    expect(router.navigate).not.toHaveBeenCalled();
  });
});

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
