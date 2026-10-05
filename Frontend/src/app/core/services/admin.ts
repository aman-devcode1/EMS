import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { AdminLoginDto, AdminProfileDto, AdminRegisterDto, ChangePasswordDto, ReplaceAdminDto, UpdateAdminProfileDto } from '../models/admin.model';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { OtpSentResponseDto, ResendOtpDto, VerifyOtpDto } from '../models/otp.model';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { TokenResponseDto } from '../models/token.model';

@Injectable({
  providedIn: 'root',
})
export class AdminService {
  constructor(private http: HttpClient) { }

  // Step 1: Admin Register (OTP Send hota hai)
  registerAdmin(data: AdminRegisterDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.ADMIN.REGISTER, data);
  }

  // Step 1: Admin Login (OTP Send hota hai)
  loginAdmin(data: AdminLoginDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.ADMIN.LOGIN, data);
  }

  // Step 2: Admin Register OTP Verify (Tokens milte hain)
  verifyRegisterOtp(data: VerifyOtpDto): Observable<ApiResponse<TokenResponseDto>> {
    return this.http.post<ApiResponse<TokenResponseDto>>(API_ENDPOINTS.ADMIN.VERIFY_REGISTRATION_OTP, data);
  }

  // Step 2: Admin Login OTP Verify (Tokens milte hain)
  verifyLoginOtp(data: VerifyOtpDto): Observable<ApiResponse<TokenResponseDto>> {
    return this.http.post<ApiResponse<TokenResponseDto>>(API_ENDPOINTS.ADMIN.VERIFY_LOGIN_OTP, data);
  }

  // Resend Otp
  resendOtp(data: ResendOtpDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.ADMIN.RESEND_OTP, data);
  }

  // Replace Admin
  replaceAdmin(data: ReplaceAdminDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.ADMIN.REPLACE_ADMIN, data);
  }

  // Get Manager
  getManager(): Observable<ApiResponse<{ managerId: number | null }>> {
    return this.http.get<ApiResponse<{ managerId: number | null }>>(API_ENDPOINTS.ADMIN.MANAGER);
  }

  // Set Manager Role
  setManagerRole(userId: number, isManager: boolean): Observable<ApiResponse<object>> {
    return this.http.patch<ApiResponse<object>>(`${API_ENDPOINTS.ADMIN.MANAGER_ROLE}/${userId}`, { isManager });
  }

  // Admin Profile
  getMyProfile(): Observable<ApiResponse<AdminProfileDto>> {
    return this.http.get<ApiResponse<AdminProfileDto>>(API_ENDPOINTS.ADMIN.ME);
  }

  // Admin Profile Update
  updateMyProfile(data: UpdateAdminProfileDto): Observable<ApiResponse<{ requiresRelogin: boolean }>> {
    return this.http.put<ApiResponse<{ requiresRelogin: boolean }>>(API_ENDPOINTS.ADMIN.ME, data);
  }

  // Admin Password Change
  changePassword(data: ChangePasswordDto): Observable<ApiResponse<{ requiresRelogin: boolean }>> {
    return this.http.post<ApiResponse<{ requiresRelogin: boolean }>>(API_ENDPOINTS.ADMIN.CHANGE_PASSWORD, data);
  }
}
