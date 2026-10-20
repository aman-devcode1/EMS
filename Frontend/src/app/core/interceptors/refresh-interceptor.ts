import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth';
import { AUTH_ENDPOINTS } from '../constants/auth-endpoints';

export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = AUTH_ENDPOINTS.some((url) => req.url.includes(url));

      // ✅ Auth endpoints skip karo — errorInterceptor unka toast handle karega
      if (isAuthEndpoint) {
        return throwError(() => error);
      }

      // Only handle 401, and skip the refresh endpoint itself
      if (error.status !== 401 || req.url.includes('/refresh-token')) {
        return throwError(() => error);
      }

      // Another refresh already in progress → wait for its result
      if (authService.isRefreshing) {
        return authService.refreshTokenSubject.pipe(
          filter((res) => res !== undefined),
          take(1),
          switchMap((res) => {
            if (!res?.data?.accessToken) {
              const err: any = new Error('Session expired.');
              err.isRefreshFailure = true;
              return throwError(() => err);
            }

            const retryReq = req.clone({
              setHeaders: { Authorization: `Bearer ${res.data.accessToken}` },
            });
            return next(retryReq);
          })
        );
      }

      // Trigger new refresh
      return authService.refreshToken().pipe(
        switchMap((res) => {
          if (!res?.data?.accessToken) {
            const err: any = new Error('Session expired. Please sign in again.');
            err.isRefreshFailure = true;
            return throwError(() => err);
          }

          const retryReq = req.clone({
            setHeaders: { Authorization: `Bearer ${res.data.accessToken}` },
          });
          return next(retryReq);
        }),
        catchError((err) => {
          (err as any).isRefreshFailure = true;
          return throwError(() => err);
        })
      );
    })
  );
};