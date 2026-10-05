import {
  HttpInterceptorFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, throwError, Observable, EMPTY, Subject } from 'rxjs';
import { catchError, filter, switchMap, take, takeUntil } from 'rxjs/operators';
import { AuthService } from '../services/auth';
import { ToastService } from '../services/toast';

// ============================================================
// MODULE-LEVEL STATE
// ============================================================
type RefreshState = 'idle' | 'refreshing' | 'logged-out';

let refreshState: RefreshState = 'idle';
const newTokenSubject = new BehaviorSubject<string | null>(null);

const logout$ = new Subject<void>();

// Auth endpoints — inpe 401 handle nahi karna
const AUTH_ENDPOINTS = [
  '/auth/login',
  '/auth/register',
  '/auth/refresh-token',
  '/auth/verify-login-otp',
  '/auth/verify-registration-otp',
  '/auth/resend-otp',
  '/auth/forgot-password',
  '/auth/reset-password',
  '/admin/login',
  '/admin/register',
  '/admin/verify-login-otp',
  '/admin/verify-registration-otp',
  '/admin/resend-otp',
  '/employees/login',
  '/employees/register',
  '/employees/verify-registration-otp',
  '/employees/resend-otp',
];

function getLoginUrl(role: string | null): string {
  if (role === 'Admin' || role === 'Manager') return '/admin/login';
  if (role === 'Employee') return '/employee/login';
  return '/admin/login';
}

// ============================================================
// INTERCEPTOR
// ============================================================
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastService = inject(ToastService);

  const token = authService.getAccessToken();
  const isAuthEndpoint = AUTH_ENDPOINTS.some((url) => req.url.includes(url));

  const authReq =
    token && !isAuthEndpoint
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if ((error as any).isRefreshFailure) {
        triggerLogout(authService, router, toastService);
        return EMPTY;
      }

      // Not 401, or auth endpoint → pass through
      if (error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      // Already logged out → silently fail (prevents duplicate logouts)
      if (refreshState === 'logged-out') {
        return EMPTY;
      }

      // Wait for refresh to complete
      if (authService.isRefreshing) {
        return authService.refreshTokenSubject.pipe(
          filter((res) => res !== null),
          take(1),
          switchMap((res) => {
            const newToken = res?.data?.accessToken;
            return next(
              authReq.clone({
                setHeaders: { Authorization: `Bearer ${newToken}` }
              })
            );
          })
        );
      }

      // First 401 → Start refresh process
      return authService.refreshToken().pipe(
        switchMap((res: any) => {
          const newToken = res?.data?.accessToken;
          if (!newToken) {
            triggerLogout(authService, router, toastService);
            return EMPTY;
          }

          // Save new refresh token if server rotated it
          if (res.data.refreshToken) {
            localStorage.setItem('refreshToken', res.data.refreshToken);
          }

          refreshState = 'idle';
          newTokenSubject.next(newToken);

          return next(
            authReq.clone({
              setHeaders: { Authorization: `Bearer ${newToken}` },
            }),
          );
        }),
        catchError(() => {
          triggerLogout(authService, router, toastService);
          return EMPTY;
        }),
      );
    }),
  );
};

// ============================================================
// LOGOUT HANDLER
// ============================================================
function triggerLogout(authService: AuthService, router: Router, toastService: ToastService): void {
  // Guard: only trigger once per burst
  if (refreshState === 'logged-out') return;

  refreshState = 'logged-out';

  const role = authService.getCurrentUserRole();
  const loginUrl = getLoginUrl(role);

  authService.logout();

  toastService.showWarning('Session expired. Please log in again.');
  router.navigate([loginUrl], {
    queryParams: { reason: 'session-expired' },
  });

  // Reset after login page has had time to mount.
  // Login page hits auth endpoints (excluded above), so this window is safe.
  setTimeout(() => {
    if (refreshState === 'logged-out') {
      refreshState = 'idle';
      newTokenSubject.next(null);
    }
  }, 3000);
}