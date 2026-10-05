import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormBuilder, FormGroup, FormsModule,
  ReactiveFormsModule, Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast';
import { AdminService } from '../../../core/services/admin';

@Component({
  selector: 'app-admin-login',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './admin-login.html',
  styleUrl: './admin-login.css',
})
export class AdminLogin {
  loginForm: FormGroup;
  isLoading = false;
  showPassword = false;

  // Random field names — browser suggestions block karne ke liye
  emailFieldName = `f_${Math.random().toString(36).substring(2)}`;
  passwordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  phoneFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private toastService: ToastService,
    private router: Router,
  ) {
    this.loginForm = this.fb.group({
      email: ['', [
        Validators.required,
        Validators.email,
        Validators.maxLength(255),
      ]],
      password: ['', [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(100),
      ]],
      phoneNumber: ['', [
        Validators.required,
        Validators.pattern('^[6-9]\\d{9}$'),
      ]],
    });
  }

  // ngOnInit(): void {
  //   this.route.queryParams.subscribe((params: any) => {
  //     if (params['reason'] === 'session-expired') {
  //       this.toastService.showInfo('Your session expired. Please sign in again.');
  //     }
  //   });
  // }

  // ============================================================
  // HELPERS — for template (cleaner HTML)
  // ============================================================
  isInvalid(field: string): boolean {
    const control = this.loginForm.get(field);
    return !!(control && control.invalid && control.touched);
  }

  getError(field: string): string {
    const control = this.loginForm.get(field);
    if (!control || !control.errors) return '';

    const errors = control.errors;

    if (field === 'email') {
      if (errors['required']) return 'Email is required.';
      if (errors['email']) return 'Please enter a valid email address (e.g. name@company.com).';
      if (errors['maxlength']) return 'Email cannot exceed 255 characters.';
    }

    if (field === 'password') {
      if (errors['required']) return 'Password is required.';
      if (errors['minlength']) return 'Password must be at least 8 characters long.';
      if (errors['maxlength']) return 'Password cannot exceed 100 characters.';
    }

    if (field === 'phoneNumber') {
      if (errors['required']) return 'Phone number is required.';
      if (errors['pattern']) return 'Enter a valid 10-digit Indian phone number starting with 6-9.';
    }

    return '';
  }

  // ============================================================
  // ACTIONS
  // ============================================================
  togglePassword(): void {
    this.showPassword = !this.showPassword;
  }

  onPhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.replace(/\D/g, '').substring(0, 10);
    input.value = value;
    this.loginForm.get('phoneNumber')?.setValue(value, { emitEvent: false });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.adminService.loginAdmin(this.loginForm.value).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastService.showSuccess(response.message);
        this.router.navigate(['/admin/verify-otp'], {
          queryParams: { email: this.loginForm.value.email, mode: 'login' },
        });
      },
      error: () => {
        this.isLoading = false;
        // Error interceptor backend का message toast में दिखाएगा
      },
    });
  }
}