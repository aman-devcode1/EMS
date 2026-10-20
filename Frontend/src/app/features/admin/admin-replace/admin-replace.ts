import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

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

  // ============================================================
  // Password Visibility
  // ============================================================
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  // ============================================================
  // Random Field Names (browser autofill block)
  // ============================================================
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
    private router: Router,
  ) {
    this.replaceForm = this.fb.group(
      {
        currentPassword: ['', [Validators.required, Validators.minLength(8)]],
        newAdminFirstName: ['', [Validators.required, Validators.maxLength(100)]],
        newAdminLastName: ['', [Validators.maxLength(100)]],
        newAdminEmail: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
        newAdminPhoneNumber: ['', [Validators.required, Validators.pattern('^[6-9]\\d{9}$')]],
        newAdminPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
        newAdminConfirmPassword: ['', [Validators.required, Validators.minLength(8)]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  // ============================================================
  // Validators
  // ============================================================
  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('newAdminPassword')?.value;
    const confirmPassword = formGroup.get('newAdminConfirmPassword')?.value;

    if (!password || !confirmPassword) return null;

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  // ============================================================
  // Toggles
  // ============================================================
  toggleNewPassword() { this.showNewPassword = !this.showNewPassword; }
  toggleConfirmPassword() { this.showConfirmPassword = !this.showConfirmPassword; }
  toggleCurrentPassword() { this.showCurrentPassword = !this.showCurrentPassword; }

  // ============================================================
  // Input Cleanup
  // ============================================================
  onPhoneInput(event: Event) {
    const input = event.target as HTMLInputElement;
    let value = input.value.replace(/\D/g, '');
    value = value.substring(0, 10);
    input.value = value;
    this.replaceForm.get('newAdminPhoneNumber')?.setValue(value, { emitEvent: false });
  }

  // ============================================================
  // SUBMIT — Send approval OTP to A → Redirect to verify-otp
  // ============================================================
  onSubmit() {
    if (this.replaceForm.invalid) {
      this.replaceForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.adminService.replaceAdmin(this.replaceForm.value).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastService.showSuccess(response.message);

        if (response.data.requiresApproval) {
          // ⚠️ sessionStorage (NOT localStorage) — kyunki isme B ki details
          // aur A/B ke passwords hain. Tab close hone par auto-cleanup ho jayega.
          // TODO (production): Backend mein "PendingAdminReplacement" table banao
          // aur passwords browser mein bilkul store mat karo.
          sessionStorage.setItem(
            'pending_replace_admin_data',
            JSON.stringify(this.replaceForm.value)
          );

          // Redirect to verify-otp with A's email + approval mode
          this.router.navigate(['/admin/verify-otp'], {
            queryParams: {
              email: response.data.nextAdminEmail,
              mode: 'approval',
            },
          });
        }
      },
      error: (err: HttpErrorResponse) => {
        this.isLoading = false;

        const msg: string = err?.error?.message ?? 'Unable to replace admin. Please try again.';
        const normalizedMsg = msg.toLowerCase();

        if (normalizedMsg.includes('current ')) {
          this.replaceForm.get('currentPassword')?.setErrors({ server: msg });
          this.replaceForm.get('currentPassword')?.markAsTouched();
          return;
        }

        if (normalizedMsg.includes('email')) {
          this.replaceForm.get('newAdminEmail')?.setErrors({ server: msg });
          this.replaceForm.get('newAdminEmail')?.markAsTouched();
          return;
        }

        this.toastService.showError(msg);
      },
    });
  }

  // ============================================================
  // Form Error Helpers
  // ============================================================
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
      if (e['minlength']) return 'Password must be at least 8 characters long.';
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
      if (e['email']) return 'Please enter a valid email address.';
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