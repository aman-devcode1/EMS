import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { ToastService } from '../services/toast';
import { catchError } from 'rxjs/internal/operators/catchError';
import { inject } from '@angular/core';
import { throwError } from 'rxjs';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toastService = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      //  skip 401 errors that are handled by the auth/refresh interceptor
      if (error.status === 401 || (error as any).isRefreshFailure) {
        return throwError(() => error);
      }

      // Back-end ka api structure: { success: false, message: "..." }
      const errorMessage = error?.error?.message || 'Something went wrong. Please try again.';
      toastService.showError(errorMessage);
      return throwError(() => error);
    }),
  );
};
