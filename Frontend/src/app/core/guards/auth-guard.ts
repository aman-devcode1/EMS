import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { TokenService } from '../services/token';
import { catchError, map, Observable, of } from 'rxjs';
import { AuthService } from '../services/auth';

@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private tokenService: TokenService,
    private router: Router,
  ) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    // Case1: Access token already in memory (fresh page transition / fast path) - allow
    if (this.tokenService.getAccessToken()) {
      return of(true);
    }

    // Case2: Access token not available (reload page) - try refresh via cookie
    return this.authService.refreshToken().pipe(
      map((response) => {
        // 204 - no session (user anonymous) - redirect
        if (!response?.data?.accessToken) {
          this.router.navigate([this.authService.getLoginUrl()]);
          return false;
        }
        // 200 - session restored, allow
        return true;
      }),
      catchError(() => {
        // Unexpected error (network fail, etc.) - redirect
        this.router.navigate([this.authService.getLoginUrl()]);
        return of(false);
      })
    );
  }
}
