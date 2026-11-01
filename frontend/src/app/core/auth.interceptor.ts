import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

// Adds "Authorization: Bearer <token>" to every API call, logs out on 401
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.token;
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      if (err.status === 401 && token) auth.logout();
      return throwError(() => err);
    })
  );
};

// Turns any API error into a readable message
export function errorMessage(err: unknown): string {
  if (err instanceof HttpErrorResponse) {
    if (err.status === 0) return 'Cannot reach the server. Is the .NET API running?';
    const body = err.error;
    if (typeof body === 'string' && body) return body;
    if (body?.errors) return Object.values(body.errors).flat().join(' ');
    if (body?.title) return body.title;
    if (err.status === 403) return 'You are not allowed to do this.';
    return `Request failed (${err.status})`;
  }
  return 'Something went wrong';
}
