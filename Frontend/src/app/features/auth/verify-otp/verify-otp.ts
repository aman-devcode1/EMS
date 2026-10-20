import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
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
import { sanitizeOtp } from '../../../core/utils/otp.util';
import { TokenService } from '../../../core/services/token';

type OtpRole = 'Admin' | 'Manager' | 'Employee';
type OtpMode = 'login' | 'register' | 'approval';

@Component({
  selector: 'app-verify-otp',
  imports: [CommonModule, ReactiveFormsModule],
  standalone: true,
  templateUrl: './verify-otp.html',
  styleUrl: './verify-otp.css',
})
export class VerifyOtp implements OnInit, OnDestroy {
  otpForm: FormGroup;
  email = '';
  mode: OtpMode = 'login';
  role: OtpRole = 'Employee';

  isLoading = false;
  isResending = false;
  resendCooldown = 0;
  private readonly RESEND_COOLDOWN_SECONDS = 60;
  private cooldownTimer: ReturnType<typeof setInterval> | null = null;

  otpFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private authService: AuthService,
    private employeeService: EmployeeService,
    private toastService: ToastService,
    private tokenService: TokenService,
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
        this.mode = 'register';
      } else {
        const modeParam = params['mode'];
        if (modeParam === 'approval') this.mode = 'approval';
        else if (modeParam === 'register') this.mode = 'register';
        else this.mode = 'login';
      }

      if (!this.email) {
        this.toastService.showError('Email is missing. Please start the process again.');
        const target = this.role === 'Employee' ? '/employee/login' : '/admin/login';
        this.router.navigate([target]);
      } else {
        // OTP abhi abhi bheja gaya hai: Resend ko pehle 60s band rakho
        // (warna backend "Please wait N seconds" ka error deta hai)
        this.startCooldown(this.RESEND_COOLDOWN_SECONDS);
      }
    });
  }

  goBack(): void {
    if (this.mode === 'approval') {
      sessionStorage.removeItem('pending_replace_admin_data');
      this.router.navigate(['/admin/replace-admin']);
      return;
    }

    // Register mode (Admin role) - back to A's dashboard (beacuse B still not active and A still Admin)
    if (this.mode === 'register' && this.role === 'Admin') {
      this.router.navigate(['/admin/dashboard']);
      return;
    }

    // Login mode / Emoloyee - back to login page
    const target = this.role === 'Employee' ? '/employee/login' : '/admin/login';
    this.router.navigate([target]);
  }

  get brandMessage(): string {
    if (this.mode === 'approval') {
      return 'Confirm the approval code from your inbox to authorize the admin replacement.';
    }
    if (this.role === 'Employee') {
      return 'One last step — confirm the code we emailed you to activate your record.';
    }
    return 'A six-digit code keeps administrator access to a single verified inbox.';
  }

  get pageHeading(): string {
    if (this.mode === 'approval') return 'Confirm approval';
    return this.role === 'Employee' ? 'Verify your email' : 'Verify one-time code';
  }

  get buttonText(): string {
    if (this.isLoading) return 'Verifying...';
    if (this.mode === 'approval') return 'Confirm approval';
    return this.role === 'Employee' ? 'Verify and activate account' : 'Verify and continue';
  }

  onOtpInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = sanitizeOtp(input.value);
    input.value = value;
    this.otpForm.get('code')?.setValue(value, { emitEvent: false });
  }

  verifyOtp(): void {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }

    if (!this.email) {
      this.toastService.showError('Email information is missing. Please start again.');
      return;
    }

    const code = this.otpForm.value.code as string;

    // APPROVAL MODE
    if (this.mode === 'approval') {
      this.handleApprovalVerify(code);
      return;
    }

    // NORMAL MODE
    const payload: VerifyOtpDto = { email: this.email, code };

    this.isLoading = true;
    this.getVerifyRequest(payload).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.otpForm.reset();
        this.toastService.showSuccess(response.message);

        if (this.mode === 'register') {
          sessionStorage.setItem('isFirstLogin', 'true');
        }

        this.authService.saveSession({ accessToken: response.data.accessToken });

        const actualRole = this.tokenService.getUserRole();

        let target = '/admin/dashboard';
        if (actualRole === 'Manager') {
          target = '/manager/dashboard';
        } else if (actualRole === 'Employee') {
          target = '/employee/dashboard';
        }

        this.router.navigate([target]);
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  private handleApprovalVerify(code: string): void {
    const raw = sessionStorage.getItem('pending_replace_admin_data');
    if (!raw) {
      this.toastService.showError(
        'Your session has expired. Please start the replacement process again.'
      );
      this.router.navigate(['/admin/replace-admin']);
      return;
    }

    let pendingData: any;
    try {
      pendingData = JSON.parse(raw);
    } catch {
      sessionStorage.removeItem('pending_replace_admin_data');
      this.toastService.showError('Your saved form data could not be read. Please start the replacement again.');
      this.router.navigate(['/admin/replace-admin']);
      return;
    }

    const payload = { ...pendingData, approvalCode: code };

    this.isLoading = true;
    this.adminService.replaceAdmin(payload).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.otpForm.reset();
        sessionStorage.removeItem('pending_replace_admin_data');
        this.toastService.showSuccess(response.message);

        if (response.data.nextAdminEmail) {
          this.router.navigate(['/admin/verify-otp'], {
            queryParams: {
              email: response.data.nextAdminEmail,
              mode: 'register',
            },
          });
        }
      },
      error: () => {
        this.isLoading = false;
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

  resendOtp(): void {
    if (this.resendCooldown > 0 || this.isResending) return;

    if (!this.email) {
      this.toastService.showError('Email information is missing. Please start again.');
      return;
    }

    // APPROVAL MODE — resend approval OTP
    if (this.mode === 'approval') {
      const raw = sessionStorage.getItem('pending_replace_admin_data');
      if (!raw) {
        this.toastService.showError('Session expired. Please start again.');
        this.router.navigate(['/admin/replace-admin']);
        return;
      }

      let pendingData: any;
      try {
        pendingData = JSON.parse(raw);
      } catch {
        sessionStorage.removeItem('pending_replace_admin_data');
        this.toastService.showError('Your saved form data could not be read. Please start the replacement again.');
        this.router.navigate(['/admin/replace-admin']);
        return;
      }

      this.isResending = true;
      this.adminService.replaceAdmin({ ...pendingData, approvalCode: undefined }).subscribe({
        next: (res) => {
          this.isResending = false;
          this.otpForm.reset();
          this.toastService.showSuccess(res.message);
          this.startCooldown(this.RESEND_COOLDOWN_SECONDS);
        },
        error: () => {
          this.isResending = false;
          this.startCooldown(5);
        },
      });
      return;
    }

    // NORMAL RESEND
    const purpose =
      this.role === 'Employee' || this.mode === 'register'
        ? OtpPurpose.Registration
        : OtpPurpose.Login;

    const payload: ResendOtpDto = { email: this.email, purpose };

    this.isResending = true;
    this.getResendRequest(payload).subscribe({
      next: (res) => {
        this.isResending = false;
        this.otpForm.reset();
        this.toastService.showSuccess(res.message);
        this.startCooldown(60);
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
    if (this.cooldownTimer) clearInterval(this.cooldownTimer);
    this.resendCooldown = seconds;
    this.cooldownTimer = setInterval(() => {
      this.resendCooldown--;
      if (this.resendCooldown <= 0 && this.cooldownTimer) {
        clearInterval(this.cooldownTimer);
        this.cooldownTimer = null;
      }
    }, 1000);
  }

  ngOnDestroy(): void {
    if (this.cooldownTimer) clearInterval(this.cooldownTimer);
  }
}