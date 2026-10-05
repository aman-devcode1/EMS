import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth';

@Component({
  selector: 'app-reset-password',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './reset-password.html',
  styleUrl: './reset-password.css',
})
export class ResetPassword implements OnInit {
  resetForm: FormGroup;
  email: string = '';
  isLoading = false;
  isResending = false;
  resendCooldown = 0;
  // 🔥 Success ke baad form ki jagah confirmation dikhate hain — role pata nahi
  // hota (Employee/Admin/Manager), isliye ek specific login page par force-navigate
  // nahi karte, dono options dikha dete hain.
  resetSuccess = false;
  resetRole: 'Admin' | 'Manager' | 'Employee' | '' = '';

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

  ngOnInit() {
    this.route.queryParams.subscribe((params) => {
      this.email = params['email'] || '';
      // Email na mile (seedha URL khola gaya) to forgot-password page par bhej do
      if (!this.email) {
        this.router.navigate(['/forgot-password']);
      }
    });
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('newPassword')?.value;
    const confirmPassword = formGroup.get('confirmNewPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onCodeInput(event: Event) {
  const input = event.target as HTMLInputElement;
  let value = input.value.replace(/\D/g, '').substring(0, 6);
  input.value = value;
  this.resetForm.get('code')?.setValue(value, { emitEvent: false });
}

  onSubmit() {
    if (this.resetForm.invalid) {
      this.toastService.showError('Please fill in all fields correctly.');
      this.resetForm.markAllAsTouched();
      return;
    }

    const rawCode = this.resetForm.value.code ?? '';
    const cleanCode = rawCode.replace(/\D/g, '');

    const payload = {
      email: this.email,
      code: cleanCode,
      newPassword: this.resetForm.value.newPassword,
      confirmNewPassword: this.resetForm.value.confirmNewPassword,
    };
    
    this.isLoading = true;
    this.authService.resetPassword(payload)
    .pipe(finalize(() => this.isLoading = false))
    .subscribe({
      next: (response) => {
        this.resetSuccess = true;
        this.resetRole = response.data.role;
        this.toastService.showSuccess(response.message);
      },
      error: () => {
        this.isLoading = false;
        // Error Interceptor khud toast dikha dega (e.g. "Invalid or expired reset code.")
      },
    });
  }

  // 🔥 Yahan "resend" asal mein forgot-password endpoint ko dobara call karta hai —
  // reset-password flow mein user already active hai, isliye generic resend-otp
  // (jo sirf unverified/registration users ke liye hai) yahan kaam nahi karega.
  resendCode() {
    if (this.resendCooldown > 0) return;

    this.isResending = true;
    this.authService.forgotPassword({ email: this.email }).subscribe({
      next: (res) => {
        this.isResending = false;
        this.toastService.showSuccess(res.message);

        this.resendCooldown = 30;
        const interval = setInterval(() => {
          this.resendCooldown--;
          if (this.resendCooldown <= 0) {
            clearInterval(interval);
          }
        }, 1000);
      },
      error: () => {
        this.isResending = false;
        this.resendCooldown = 5;
      },
    });
  }
}
