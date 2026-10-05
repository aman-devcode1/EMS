import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { ActivatedRoute, Router } from '@angular/router';
import { ResendOtpDto, VerifyOtpDto } from '../../../core/models/otp.model';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../core/models/api-response.model';
import { TokenResponseDto } from '../../../core/models/token.model';
import { OtpPurpose } from '../../../core/enums/otp-purpose';
import { EmployeeService } from '../../../core/services/employee';
import { AuthService } from '../../../core/services/auth';

type OtpRole = 'Admin' | 'Manager' | 'Employee';

@Component({
  selector: 'app-verify-otp',
  imports: [CommonModule, ReactiveFormsModule],
  standalone: true,
  templateUrl: './verify-otp.html',
  styleUrl: './verify-otp.css',
})
export class VerifyOtp implements OnInit {
  otpForm: FormGroup;
  email = '';
  mode: 'login' | 'register' = 'login';
  role: OtpRole = 'Employee';

  isLoading = false;
  isResending = false;
  resendCooldown = 0;

  otpFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private authService: AuthService,
    private employeeService: EmployeeService,
    private toastService: ToastService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.otpForm = this.fb.group({
      code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
    });
  }

  ngOnInit(): void {
    this.role = (this.route.snapshot.data['role'] as OtpRole) ?? 'Employee';

    this.route.queryParams.subscribe((params) => {
      this.email = params['email'] || '';

      if (this.role === 'Employee') {
        // Always 'register' mode for Employee
        this.mode = 'register';
      } else {
        const modeParam = params['mode'];
        this.mode = modeParam === 'register' ? 'register' : 'login';
      }

      if (!this.email) {
        this.toastService.showError('Email is missing. Please start the process again.');
        const target = this.role === 'Employee' ? '/employee/login' : '/admin/login';
        this.router.navigate([target]);
      }
    });
  }

  // Back navigation
  goBack(): void {
    if (window.history.length > 1) {
      window.history.back();
    } else {
      const target = this.role === 'Employee' ? '/employee/login' : '/admin/login';
      this.router.navigate([target]);
    }
  }

  // Dynamic text — according to role/mode
  get brandMessage(): string {
    if (this.role === 'Employee') {
      return 'One last step — confirm the code we emailed you to activate your record.';
    }
    return 'A six-digit code keeps administrator access to a single verified inbox.';
  }

  get pageHeading(): string {
    return this.role === 'Employee' ? 'Verify your email' : 'Verify one-time code';
  }

  get buttonText(): string {
    if (this.isLoading) return 'Verifying...';
    return this.role === 'Employee' ? 'Verify and activate account' : 'Verify and continue';
  }

  // Input cleanup
  onOtpInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.replace(/\D/g, '').substring(0, 6);
    input.value = value;
    this.otpForm.get('code')?.setValue(value, { emitEvent: false });
  }

  // Verify OTP
  verifyOtp(): void {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }

    if (!this.email) {
      this.toastService.showError('Email information is missing. Please start again.');
      return;
    }

    const payload: VerifyOtpDto = {
      email: this.email,
      code: this.otpForm.value.code,
    };

    this.isLoading = true;
    this.getVerifyRequest(payload).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastService.showSuccess(response.message);

        this.authService.saveSession({
          accessToken: response.data.accessToken,
        });

        const target = this.role === 'Employee' ? '/employee/dashboard' : '/admin/dashboard';
        this.router.navigate([target]);
      },
      error: () => {
        this.isLoading = false;
        // Error interceptor handles the toast
      },
    });
  }

  private getVerifyRequest(payload: VerifyOtpDto): Observable<ApiResponse<TokenResponseDto>> {
    if (this.role === 'Employee') {
      return this.employeeService.verifyRegistrationOtp(payload);
    }
    return this.mode === 'login'
      ? this.adminService.verifyLoginOtp(payload)
      : this.adminService.verifyRegisterOtp(payload);
  }

  // Resend OTP
  resendOtp(): void {
    if (this.resendCooldown > 0 || this.isResending) return;

    if (!this.email) {
      this.toastService.showError('Email information is missing. Please start again.');
      return;
    }

    const purpose =
      this.role === 'Employee' || this.mode === 'register'
        ? OtpPurpose.Registration
        : OtpPurpose.Login;

    const payload: ResendOtpDto = { email: this.email, purpose };

    this.isResending = true;
    this.getResendRequest(payload).subscribe({
      next: (res) => {
        this.isResending = false;
        this.toastService.showSuccess(res.message);
        this.startCooldown(30);
      },
      error: () => {
        this.isResending = false;
        this.startCooldown(5);
      },
    });
  }

  private getResendRequest(payload: ResendOtpDto): Observable<ApiResponse<{ email: string }>> {
    if (this.role === 'Employee') {
      return this.employeeService.resendOtp(payload);
    }
    return this.adminService.resendOtp(payload);
  }

  private startCooldown(seconds: number): void {
    this.resendCooldown = seconds;
    const interval = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0) clearInterval(interval);
    }, 1000);
  }
}