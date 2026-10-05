import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast';
import { AdminService } from '../../../core/services/admin';

@Component({
  selector: 'app-admin-register',
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './admin-register.html',
  styleUrl: './admin-register.css',
})
export class AdminRegister {
  registerForm: FormGroup;
  isLoading = false;

  showPassword = false;
  showConfirmPassword = false;

  // Random field names — browser suggestions block karne ke liye
  regFirstNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  regLastNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  regEmailFieldName = `f_${Math.random().toString(36).substring(2)}`;
  regPhoneFieldName = `f_${Math.random().toString(36).substring(2)}`;
  regPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  regConfirmPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private toastService: ToastService,
    private router: Router,
  ) {
    this.registerForm = this.fb.group(
      {
        firstName: ['', [Validators.required, Validators.maxLength(100)]],
        lastName: ['', [Validators.maxLength(100)]],
        email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
        phoneNumber: ['', [Validators.required, Validators.pattern('^[6-9]\\d{9}$')]],
        password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
        confirmPassword: ['', [Validators.required, Validators.minLength(8)]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  // ============================================================
  // VALIDATORS
  // ============================================================
  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('password')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    if (!password || !confirmPassword) return null;

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  // ============================================================
  // HELPERS
  // ============================================================
  isInvalid(field: string): boolean {
    const c = this.registerForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  getError(field: string): string {
    const c = this.registerForm.get(field);
    if (!c || !c.errors) return '';
    const e = c.errors;

    if (field === 'firstName') {
      if (e['required']) return 'First name is required.';
      if (e['maxlength']) return 'First name cannot exceed 100 characters.';
    }
    if (field === 'lastName') {
      if (e['maxlength']) return 'Last name cannot exceed 100 characters.';
    }
    if (field === 'email') {
      if (e['required']) return 'Email is required.';
      if (e['email']) return 'Please enter a valid email address (e.g. name@company.com).';
      if (e['maxlength']) return 'Email cannot exceed 255 characters.';
    }
    if (field === 'phoneNumber') {
      if (e['required']) return 'Phone number is required.';
      if (e['pattern']) return 'Enter a valid 10-digit Indian phone number starting with 6-9.';
    }
    if (field === 'password') {
      if (e['required']) return 'Password is required.';
      if (e['minlength']) return 'Password must be at least 8 characters long.';
      if (e['maxlength']) return 'Password cannot exceed 100 characters.';
    }
    if (field === 'confirmPassword') {
      if (e['required']) return 'Please confirm your password.';
      if (e['minlength']) return 'Confirm password must be at least 8 characters long.';
    }

    return '';
  }

  // ============================================================
  // TOGGLES
  // ============================================================
  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPassword() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  // ============================================================
  // INPUT CLEANUP
  // ============================================================
  onPhoneInput(event: Event) {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\D/g, '').substring(0, 10);
    input.value = value;
    this.registerForm.get('phoneNumber')?.setValue(value, { emitEvent: false });
  }

  // ============================================================
  // SUBMIT
  // ============================================================
  onSubmit() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.adminService.registerAdmin(this.registerForm.value).subscribe({
      next: (response) => {
        this.toastService.showSuccess(response.message);
        this.isLoading = false;
        this.router.navigate(['/admin/verify-otp'], {
          queryParams: { email: this.registerForm.value.email, mode: 'register' },
        });
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }
}