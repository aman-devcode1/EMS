import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { ToastService } from '../services/toast';
import { catchError } from 'rxjs/internal/operators/catchError';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';
import { AUTH_ENDPOINTS } from '../constants/auth-endpoints';

// Backend ka asli karan nikalta hai. Generic "Something went wrong" kabhi nahi.
function getErrorMessage(error: HttpErrorResponse): string {
  // Server tak pahunch hi nahi paye (backend band / internet nahi)
  if (error.status === 0) {
    return 'Cannot reach the server. Please check your internet connection or try again in a moment.';
  }

  const body = error.error;

  // 1. Backend ApiResponse: { success, message }
  if (body && typeof body === 'object') {
    if (typeof body.message === 'string' && body.message.trim()) return body.message;

    // ASP.NET auto validation: { errors: { Field: ["msg"] } }
    if (body.errors && typeof body.errors === 'object') {
      const first = (Object.values(body.errors) as string[][]).flat()[0];
      if (typeof first === 'string' && first.trim()) return first;
    }
    if (typeof body.title === 'string' && body.title.trim()) return body.title;
  }
  if (typeof body === 'string' && body.trim() && body.length < 200) return body;

  // 2. Body khaali hai: status se asli karan
  switch (error.status) {
    case 400: return 'The request was not valid. Please check the details you entered.';
    case 401: return 'Your session is not valid. Please sign in again.';
    case 403: return 'You do not have permission to do this.';
    case 404: return 'The requested item or service was not found.';
    case 409: return 'This conflicts with data that already exists.';
    case 413: return 'The data you sent is too large.';
    case 429: {
      const wait = Number(error.headers?.get('Retry-After'));
      return wait > 0
        ? `Too many attempts. Please try again in ${wait} seconds.`
        : 'Too many attempts. Please wait a little and try again.';
    }
    case 502:
    case 503:
    case 504: return 'The server is temporarily unavailable. Please try again shortly.';
    default: return `The request failed (HTTP ${error.status}). Please try again.`;
  }
}

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = AUTH_ENDPOINTS.some((url) => req.url.includes(url));

      if (isAuthEndpoint) {
        // Unverified account: error nahi, guidance. Backend ka apna message dikhao.
        if (error.status === 409 && (req.url.includes('/auth/login') || req.url.includes('/admin/login'))) {
          toastService.showInfo(getErrorMessage(error));
          return throwError(() => error);
        }
        toastService.showError(getErrorMessage(error));
        return throwError(() => error);
      }

      // 401 ko auth/refresh interceptor sambhalta hai
      if (error.status === 401 || (error as any).isRefreshFailure) {
        return throwError(() => error);
      }

      toastService.showError(getErrorMessage(error));
      return throwError(() => error);
    }),
  );
};