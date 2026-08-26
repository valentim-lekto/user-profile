import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { AUTH_TOKEN_STORAGE_KEY } from './auth.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let router: Router;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideRouter([])],
    });
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it.each([
    ['absent', null],
    ['malformed', 'not-a-jwt'],
    ['expired', createToken(Math.floor(Date.now() / 1000) - 1)],
  ])('returns a login UrlTree for an %s token', (_case, accessToken) => {
    if (accessToken) {
      sessionStorage.setItem(AUTH_TOKEN_STORAGE_KEY, accessToken);
    }

    const result = runGuard();

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/login');
    expect(sessionStorage.getItem(AUTH_TOKEN_STORAGE_KEY)).toBeNull();
  });

  it('allows a token whose exp is still in the future', () => {
    sessionStorage.setItem(
      AUTH_TOKEN_STORAGE_KEY,
      createToken(Math.floor(Date.now() / 1000) + 60),
    );

    expect(runGuard()).toBe(true);
  });

  function runGuard(): boolean | UrlTree {
    return TestBed.runInInjectionContext(
      () =>
        authGuard(
          {} as ActivatedRouteSnapshot,
          {} as RouterStateSnapshot,
        ) as boolean | UrlTree,
    );
  }
});

function createToken(exp: number): string {
  return `${encodeJwtPart({ alg: 'HS256' })}.${encodeJwtPart({ exp })}.synthetic`;
}

function encodeJwtPart(value: object): string {
  return btoa(JSON.stringify(value)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
