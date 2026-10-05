import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import {
  ForgotPassowrdDto,
  ResetPassowrdDto,
  ResetPasswordResponse,
} from '../models/password-reset.model';
import { ApiResponse } from '../models/api-response.model';
import { OtpSentResponseDto } from '../models/otp.model';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { TokenService } from './token';
import { catchError, map, Observable, Subject, take, tap, throwError } from 'rxjs';
import { LOGIN_URL_BY_ROLE, ROUTES } from '../constants/routes';

@Injectable({
  providedIn: 'root',
})
export class AuthService {

  // ============================================================
  // REFRESH STATE
  // ============================================================
  public isRefreshing = false;
  public refreshFailed = false;
  public refreshTokenSubject = new Subject<ApiResponse<{ accessToken: string; expiresIn: number; tokenType: string }> | null>();

  constructor(
    private http: HttpClient,
    private tokenService: TokenService
  ) { }

  // ============================================================
  // PASSWORD RESET
  // ============================================================
  forgotPassword(data: ForgotPassowrdDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(
      API_ENDPOINTS.AUTH.FORGOT_PASSWORD,
      data
    );
  }

  resetPassword(data: ResetPassowrdDto): Observable<ApiResponse<ResetPasswordResponse>> {
    return this.http.post<ApiResponse<ResetPasswordResponse>>(
      API_ENDPOINTS.AUTH.RESET_PASSWORD,
      data
    );
  }

  // ============================================================
  // TOKEN ACCESS
  // ============================================================
  getAccessToken(): string | null {
    return this.tokenService.getAccessToken();
  }

  getCurrentUserRole(): string | null {
    return this.tokenService.getUserRole();
  }

  // ============================================================
  // SAVE SESSION (login/OTP verify ke baad)
  // ============================================================
  saveSession(data: { accessToken: string }): void {
    this.refreshFailed = false;
    this.tokenService.setAccessToken(data.accessToken);
    const role = this.tokenService.getUserRole();
    if (role)
      this.tokenService.setLastRole(role);
  }

  // ============================================================
  // CENTRAL: Get login URL based on role
  // ============================================================
  getLoginUrl(): string {
    // Priority: current session role - last known role - default
    const currentRole = this.tokenService.getUserRole();
    const role = currentRole ?? this.tokenService.getLastRole();

    // Map role - login url (defined in routes.ts)
    return LOGIN_URL_BY_ROLE[role ?? ''] ?? `/${ROUTES.ADMIN_LOGIN}`;
  }

  // ============================================================
  // REFRESH TOKEN
  // - 200: session restored → returns ApiResponse
  // - 204: no session (silent) → returns null
  // ============================================================
  refreshToken(): Observable<ApiResponse<{ accessToken: string; expiresIn: number; tokenType: string }> | null> {
    // Already failed → don't retry
    if (this.refreshFailed) {
      return throwError(() => new Error('Session expired. Please log in again.'));
    }

    // Refresh in progress → wait for it
    if (this.isRefreshing) {
      return this.refreshTokenSubject.pipe(take(1));
    }

    this.isRefreshing = true;

    return this.http
      .post<any>(API_ENDPOINTS.AUTH.REFRESH, {}, { observe: 'response' })
      .pipe(
        tap((httpResponse) => {
          this.isRefreshing = false;

          // ✅ 204 or empty body → no session, emit null silently
          if (
            httpResponse.status === 204 ||
            !httpResponse.body?.data?.accessToken
          ) {
            this.refreshTokenSubject.next(null);
            return;
          }

          // ✅ 200 → store access token, notify waiting subscribers
          this.tokenService.setAccessToken(httpResponse.body.data.accessToken);
          this.refreshTokenSubject.next(httpResponse.body);
        }),
        map((httpResponse) => {
          // 204 or empty → null (no session)
          if (
            httpResponse.status === 204 ||
            !httpResponse.body?.data?.accessToken
          ) {
            return null;
          }
          return httpResponse.body;
        }),
        catchError((error) => {
          this.isRefreshing = false;
          this.refreshFailed = true;
          this.refreshTokenSubject.error(error);
          this.refreshTokenSubject = new Subject<any>();
          return throwError(() => error);
        })
      );
  }

  // ============================================================
  // LOGOUT
  // ============================================================
  logout(): Observable<any> {
    return this.http.post<any>(API_ENDPOINTS.AUTH.LOGOUT, {}).pipe(
      tap(() => this.clearLocalSession()),
      catchError((err) => {
        this.clearLocalSession();
        return throwError(() => err);
      })
    );
  }

  clearLocalSession(): void {
    this.tokenService.clearAccessToken();
    this.refreshFailed = false;
    this.isRefreshing = false;
    this.refreshTokenSubject = new Subject();
  }
}