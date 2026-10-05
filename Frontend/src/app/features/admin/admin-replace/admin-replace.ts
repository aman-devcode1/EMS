import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { TokenService } from '../../../core/services/token';
import { Router, RouterLink } from '@angular/router';

@Component({
  selector: 'app-admin-replace',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './admin-replace.html',
  styleUrl: './admin-replace.css',
})
export class ReplaceAdmin {
  replaceForm: FormGroup;
  isLoading = false;

  // Password visibility
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  // Random field names — browser suggestions block karne ke liye
  raCurrentPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  raFirstNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  raLastNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  raEmailFieldName = `f_${Math.random().toString(36).substring(2)}`;
  raPhoneFieldName = `f_${Math.random().toString(36).substring(2)}`;
  raNewPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  raConfirmNewPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private toastService: ToastService,
    private tokenService: TokenService,
    private router: Router,
  ) {
    this.replaceForm = this.fb.group(
      {
        currentPassword: ['', [Validators.required]],
        newAdminFirstName: ['', [Validators.required, Validators.maxLength(100)]],
        newAdminLastName: ['', [Validators.maxLength(100)]],
        newAdminEmail: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
        newAdminPhoneNumber: ['', [Validators.required, Validators.pattern('^[6-9]\\d{9}$')]],
        newAdminPassword: ['', [Validators.required, Validators.minLength(8)]],
        newAdminConfirmPassword: ['', [Validators.required, Validators.minLength(8)]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('newAdminPassword')?.value;
    const confirmPassword = formGroup.get('newAdminConfirmPassword')?.value;

    if (!password || !confirmPassword) return null;

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  toggleNewPassword() {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPassword() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  toggleCurrentPassword() {
    this.showCurrentPassword = !this.showCurrentPassword;
  }

  // Phone number ko sirf digit tak limit karna hai (no alphabates, spaces, extra didgits)
  onPhoneInput(event: Event) {
    const input = event.target as HTMLInputElement;

    let value = input.value.replace(/\D/g, '');

    value = value.substring(0, 10);

    input.value = value;

    this.replaceForm.get('newAdminPhoneNumber')?.setValue(value, { emitEvent: false });
  }

  onSubmit() {
    if (this.replaceForm.invalid) {
      // this.toastService.showError('Please fill in all required fields correctly.');
      this.replaceForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.adminService.replaceAdmin(this.replaceForm.value).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastService.showSuccess(response.message);

        // 🔥 Backend ne humare (purane admin ke) tokens already revoke kar diye hain —
        // isliye frontend ki state ko bhi turant sync karna zaroori hai.
        this.tokenService.clearTokens();
        this.router.navigate(['/admin/verify-otp'], {
          queryParams: { email: this.replaceForm.value.newAdminEmail, mode: 'register' },
        });
      },
      error: () => {
        this.isLoading = false;
        // Error toast pehle se hi error-interceptor.ts dikha dega — yahan dobara nahi
      },
    });
  }

  isInvalid(field: string): boolean {
    const c = this.replaceForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  getError(field: string): string {
    const c = this.replaceForm.get(field);
    if (!c || !c.errors) return '';
    const e = c.errors;

    if (field === 'currentPassword') {
      if (e['required']) return 'Current password is required.';
    }
    if (field === 'newAdminFirstName') {
      if (e['required']) return 'First name is required.';
      if (e['maxlength']) return 'First name cannot exceed 100 characters.';
    }
    if (field === 'newAdminLastName') {
      if (e['maxlength']) return 'Last name cannot exceed 100 characters.';
    }
    if (field === 'newAdminEmail') {
      if (e['required']) return 'Email is required.';
      if (e['email']) return 'Please enter a valid email address (e.g. name@company.com).';
      if (e['maxlength']) return 'Email cannot exceed 255 characters.';
    }
    if (field === 'newAdminPhoneNumber') {
      if (e['required']) return 'Phone number is required.';
      if (e['pattern']) return 'Enter a valid 10-digit Indian phone number starting with 6-9.';
    }
    if (field === 'newAdminPassword') {
      if (e['required']) return 'Password is required.';
      if (e['minlength']) return 'Password must be at least 8 characters long.';
      if (e['maxlength']) return 'Password cannot exceed 100 characters.';
    }
    if (field === 'newAdminConfirmPassword') {
      if (e['required']) return 'Please confirm the password.';
      if (e['minlength']) return 'Confirm password must be at least 8 characters long.';
    }

    return '';
  }
}
