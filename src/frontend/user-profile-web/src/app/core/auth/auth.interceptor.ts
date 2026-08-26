import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

const PROTECTED_REQUESTS = new Set([
  'GET /api/profile',
  'PUT /api/profile',
  'PUT /api/profile/password',
]);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (!PROTECTED_REQUESTS.has(`${request.method.toUpperCase()} ${request.urlWithParams}`)) {
    return next(request);
  }

  const auth = inject(AuthService);
  const accessToken = auth.getValidAccessToken();

  if (!accessToken) {
    return next(request);
  }

  const router = inject(Router);
  const authenticatedRequest = request.clone({
    setHeaders: { Authorization: `Bearer ${accessToken}` },
  });

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        auth.isCurrentAccessToken(accessToken)
      ) {
        auth.clearSession();
        void router.navigate(['/login']);
      }

      return throwError(() => error);
    }),
  );
};
