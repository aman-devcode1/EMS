import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { OtpSentResponseDto, ResendOtpDto } from '../models/otp.model';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import { API_ENDPOINTS } from '../constants/api-endpoints';

@Injectable({
  providedIn: 'root',
})
export class OtpService {
  constructor(private http: HttpClient) {}

  // Resend otp (Registration OTP ya Login OTP dono ke liye same)
  resendOtp(data: ResendOtpDto): Observable<ApiResponse<OtpSentResponseDto>> {
    return this.http.post<ApiResponse<OtpSentResponseDto>>(API_ENDPOINTS.ADMIN.RESEND_OTP, data);
  }
}
