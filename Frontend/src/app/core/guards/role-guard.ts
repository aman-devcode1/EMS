import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
import { TokenService } from '../services/token';
import { AuthService } from '../services/auth';
import { catchError, map, Observable, of } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class RoleGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private tokenService: TokenService,
    private router: Router,
  ) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    const requiredRole = route.data['roles']; // Get the role from required route

    // Case 1: Token already in memory - check role directly
    if (this.tokenService.getAccessToken())
      return of(this.verifyRole(requiredRole));

    // Case 2: Page Reload - refresh, then check role
    return this.authService.refreshToken().pipe(
      map((response) => {
        if (!response?.data?.accessToken) {
          this.router.navigate([this.authService.getLoginUrl()]);
          return false;
        }
        return this.verifyRole(requiredRole);
      }),
      catchError(() => {
        this.router.navigate([this.authService.getLoginUrl()]);
        return of(false);
      })
    );
  }

  private verifyRole(requiredRole: string | undefined): boolean {
    const userRole = this.tokenService.getUserRole();

    if (userRole?.toLowerCase() === requiredRole?.toLowerCase()) {
      return true;
    }
    // Role mismatch - bounce to login
    this.router.navigate([this.authService.getLoginUrl()]);
    return false;
  }
}
