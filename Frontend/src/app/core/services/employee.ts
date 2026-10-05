import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { ApiResponse } from '../models/api-response.model';
import { RegisterDto, LoginDto, EmployeeResponseDto, UpdateEmployeeProfileDto, ChangeEmployeePasswordDto, UpdateEmployeeProfessionalDto } from '../models/employee.model';
import { OtpSentResponseDto, VerifyOtpDto, ResendOtpDto } from '../models/otp.model';
import { API_ENDPOINTS } from '../constants/api-endpoints';
import { TokenResponseDto } from '../models/token.model';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root',
})
export class EmployeeService {
  constructor(private http: HttpClient) { }

  // Employee Register — OTP bhejta hai (Admin jaisa hi pattern)
  registerEmployee(data: RegisterDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.EMPLOYEE.REGISTER, data);
  }

  // Employee Login — seedha tokens milte hain, OTP nahi
  loginEmployee(data: LoginDto): Observable<ApiResponse<TokenResponseDto>> {
    return this.http.post<ApiResponse<TokenResponseDto>>(API_ENDPOINTS.EMPLOYEE.LOGIN, data);
  }

  // Employee Registration OTP Verify — tokens milte hain
  verifyRegistrationOtp(data: VerifyOtpDto): Observable<ApiResponse<TokenResponseDto>> {
    return this.http.post<ApiResponse<TokenResponseDto>>(API_ENDPOINTS.EMPLOYEE.VERIFY_REGISTRATION_OTP, data);
  }

  // Employee Resend OTP — 🔥 apna alag AUTH.RESEND_OTP use karta hai, ADMIN.RESEND_OTP nahi
  resendOtp(data: ResendOtpDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.EMPLOYEE.RESEND_OTP, data);
  }

  // Backend se Employees ka UserId get karna hai
  getMyProfile(): Observable<ApiResponse<EmployeeResponseDto>> {
    return this.http.get<ApiResponse<EmployeeResponseDto>>(API_ENDPOINTS.EMPLOYEE.GET_MY_PROFILE);
  }

  // Admin Dashboard के लिए — सारे Employees पेजिनेशन के साथ
  getAll(pageNumber: number = 1, pageSize: number = 500): Observable<ApiResponse<any>> {
    return this.http.get<ApiResponse<any>>(`${API_ENDPOINTS.EMPLOYEE.GET_ALL}?pageNumber=${pageNumber}&pageSize=${pageSize}`);
  }

  // Update on profile (Employee)
  updateMyProfile(data: UpdateEmployeeProfileDto): Observable<ApiResponse<{ requiresRelogin: boolean }>> {
    return this.http.put<ApiResponse<{ requiresRelogin: boolean }>>(`${API_ENDPOINTS.EMPLOYEE.GET_MY_PROFILE}`, data);
  }

  // Changer own password (Employee)
  changePassword(data: ChangeEmployeePasswordDto): Observable<ApiResponse<{ requiresRelogin: boolean }>> {
    return this.http.post<ApiResponse<{ requiresRelogin: boolean }>>(`${API_ENDPOINTS.EMPLOYEE.GET_MY_PROFILE}/change-password`, data);
  }

  // Single employee detail (Open detail page when click by admin in admin dashboard)
  getById(id: number): Observable<ApiResponse<EmployeeResponseDto>> {
    return this.http.get<ApiResponse<EmployeeResponseDto>>(`${API_ENDPOINTS.EMPLOYEE.GET_ALL}/${id}`);
  }

  updateProfessionalInfo(id: number, data: UpdateEmployeeProfessionalDto): Observable<ApiResponse<EmployeeResponseDto>> {
    return this.http.put<ApiResponse<EmployeeResponseDto>>(`${API_ENDPOINTS.EMPLOYEE.GET_ALL}/${id}/professional`, data);
  }
}
