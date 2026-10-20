import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, Subscription } from 'rxjs';
import { ToastService } from '../../../core/services/toast';
import { AuthService } from '../../../core/services/auth';
import { sanitizeOtp } from '../../../core/utils/otp.util';

// Backend OtpSettings.ResendCooldownSeconds = 60 se match karta hai.
const RESEND_COOLDOWN_SECONDS = 60;
// Resend fail hone par chhota cooldown, taaki user dobara try kar sake.
const RESEND_FAILED_COOLDOWN_SECONDS = 5;

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit, OnDestroy {
  resetForm: FormGroup;
  email = '';
  isLoading = false;
  isResending = false;
  resendCooldown = 0;
  resetSuccess = false;
  resetRole: 'Admin' | 'Manager' | 'Employee' | '' = '';

  private cooldownTimer: ReturnType<typeof setInterval> | null = null;
  private paramsSub?: Subscription;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastService: ToastService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.resetForm = this.fb.group(
      {
        code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]],
        newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
        confirmNewPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  ngOnInit(): void {
    this.paramsSub = this.route.queryParams.subscribe((params) => {
      this.email = params['email'] || '';
      if (!this.email) {
        this.router.navigate(['/forgot-password']);
      }
    });
  }

  ngOnDestroy(): void {
    this.paramsSub?.unsubscribe();
    if (this.cooldownTimer) clearInterval(this.cooldownTimer);
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmNewPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onCodeInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = sanitizeOtp(input.value);
    input.value = value;
    this.resetForm.get('code')?.setValue(value, { emitEvent: false });
  }

  onSubmit(): void {
    if (this.resetForm.invalid) {
      this.toastService.showError('Please fill in all fields correctly.');
      this.resetForm.markAllAsTouched();
      return;
    }

    const payload = {
      email: this.email,
      code: sanitizeOtp(this.resetForm.value.code),
      newPassword: this.resetForm.value.newPassword,
      confirmNewPassword: this.resetForm.value.confirmNewPassword,
    };

    this.isLoading = true;
    this.authService.resetPassword(payload)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (response) => {
          this.resetSuccess = true;
          this.resetRole = response.data.role;
          this.toastService.showSuccess(response.message);
        },
        // Error toast interceptor dikhata hai.
        error: () => {},
      });
  }

  // Reset flow mein resend asal mein forgot-password endpoint ko call karta hai.
  resendCode(): void {
    if (this.resendCooldown > 0 || this.isResending) return;

    this.isResending = true;
    this.authService.forgotPassword({ email: this.email })
      .pipe(finalize(() => (this.isResending = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.startCooldown(RESEND_COOLDOWN_SECONDS);
        },
        // Error toast interceptor dikhata hai. Button thodi der baad enable ho jayega.
        error: () => this.startCooldown(RESEND_FAILED_COOLDOWN_SECONDS),
      });
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
}