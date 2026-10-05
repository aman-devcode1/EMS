import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth';

export const refreshInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      // Only handle 401, and skip the refresh endpoint itself
      if (error.status !== 401 || req.url.includes('/refresh-token')) {
        return throwError(() => error);
      }

      // Another refresh already in progress → wait for its result
      if (authService.isRefreshing) {
        return authService.refreshTokenSubject.pipe(
          filter((res) => res !== undefined),   // ✅ null pass-through (for 204)
          take(1),
          switchMap((res) => {
            // 204 (null) → session gone, mark failure
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
          // 204 (null) → no session, mark as refresh failure
          if (!res?.data?.accessToken) {
            const err: any = new Error('Session expired.');
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