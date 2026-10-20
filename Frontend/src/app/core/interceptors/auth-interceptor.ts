import {
  HttpInterceptorFn,
  HttpErrorResponse,
} from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, throwError, EMPTY, Subject } from 'rxjs';
import { catchError, filter, switchMap, take } from 'rxjs/operators';
import { AuthService } from '../services/auth';
import { ToastService } from '../services/toast';
import { TokenService } from '../services/token'; // 👈 Import TokenService

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

// ============================================================
// INTERCEPTOR
// ============================================================
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toastService = inject(ToastService);
  const tokenService = inject(TokenService); // 👈 Inject TokenService

  const token = authService.getAccessToken();
  const isAuthEndpoint = AUTH_ENDPOINTS.some((url) => req.url.includes(url));

  const authReq =
    token && !isAuthEndpoint
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if ((error as any).isRefreshFailure) {
        triggerLogout(authService, router, toastService, tokenService);
        return EMPTY;
      }

      if (error.status !== 401 || isAuthEndpoint) {
        return throwError(() => error);
      }

      if (refreshState === 'logged-out') {
        return EMPTY;
      }

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

      return authService.refreshToken().pipe(
        switchMap((res: any) => {
          const newToken = res?.data?.accessToken;
          if (!newToken) {
            triggerLogout(authService, router, toastService, tokenService);
            return EMPTY;
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
          triggerLogout(authService, router, toastService, tokenService);
          return EMPTY;
        }),
      );
    }),
  );
};

// ============================================================
// LOGOUT HANDLER
// ============================================================
function triggerLogout(
  authService: AuthService,
  router: Router,
  toastService: ToastService,
  tokenService: TokenService
): void {
  if (refreshState === 'logged-out') return;

  refreshState = 'logged-out';

  // ✅ Yahan hum 'ems_last_role' use kar rahe hain, current token nahi
  const lastRole = tokenService.getLastRole();
  let target = '/'; // Default landing page

  if (lastRole === 'Admin' || lastRole === 'Manager') target = '/admin/login';
  else if (lastRole === 'Employee') target = '/employee/login';

  authService.logout();

  // toastService.showWarning('Session expired. Please log in again.');
  router.navigate([target], {
    queryParams: { reason: 'session-expired' },
  });

  setTimeout(() => {
    if (refreshState === 'logged-out') {
      refreshState = 'idle';
      newTokenSubject.next(null);
    }
  }, 3000);
}