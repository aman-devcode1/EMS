import { APP_INITIALIZER, ApplicationConfig, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './core/interceptors/auth-interceptor';
import { errorInterceptor } from './core/interceptors/error-interceptor';
import { refreshInterceptor } from './core/interceptors/refresh-interceptor';
import { AuthService } from './core/services/auth';
import { catchError, firstValueFrom, of } from 'rxjs';

// ✅ App startup par ek baar refresh try karo
function initializeAuth(authService: AuthService) {
  return () => {
    return firstValueFrom(
      authService.refreshToken().pipe(
        catchError(() => of(null))   // fail ho to ignore
      )
    );
  };
}

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(
      withInterceptors([authInterceptor, refreshInterceptor, errorInterceptor])
    )
  ]
};