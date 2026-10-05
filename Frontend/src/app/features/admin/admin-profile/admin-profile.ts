import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { TokenService } from '../../../core/services/token';
import { ChangePasswordDto, UpdateAdminProfileDto } from '../../../core/models/admin.model';
import { AuthService } from '../../../core/services/auth';

type ViewMode = 'details' | 'edit' | 'password';

@Component({
  selector: 'app-admin-profile',
  imports: [CommonModule, ReactiveFormsModule],
  standalone: true,
  templateUrl: './admin-profile.html',
  styleUrl: './admin-profile.css',
})
export class AdminProfile implements OnInit {
  profileForm: FormGroup;
  passwordForm: FormGroup;

  isLoading = true;
  isSaving = false;
  isChangingPassword = false;

  profile: any = {};
  viewMode: ViewMode = 'details';

  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  minDob: string;
  maxDob: string;

  // Random field names — browser suggestions block karne ke liye
  // Profile form
  profFirstNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profLastNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profEmailFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profPhoneFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profDobFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profAddressFieldName = `f_${Math.random().toString(36).substring(2)}`;

  // Password form
  profCurrentPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profNewPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  profConfirmNewPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private adminService: AdminService,
    private authService: AuthService,
    private toastService: ToastService,
    private tokenService: TokenService,
    private router: Router,
  ) {
    // Profile form
    this.profileForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^[6-9]\d{9}$/)]],
      presentAddress: ['', [Validators.maxLength(500)]],
      dateOfBirth: ['', [this.dateRangeValidator]],
    });

    // Password form
    this.passwordForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      confirmNewPassword: ['', Validators.required],
    }, { validators: [this.passwordMatchValidator, this.passwordSameAsCurrentValidator] });

    const currentYear = new Date().getFullYear();
    this.minDob = `${currentYear - 100}-01-01`;
    this.maxDob = `${currentYear - 18}-12-31`;
  }

  ngOnInit(): void {
    this.loadProfile();
  }

  dateRangeValidator(control: any) {
    const value = control.value;
    if (!value) return null;

    // Format - "YYYY-MM-DD"
    const year = parseInt(value.substring(0, 4), 10);
    const currentYear = new Date().getFullYear();

    if (isNaN(year) || value.substring(0, 4).length != 4) return { invalidDate: true };
    if (year < currentYear - 100) return { yearTooEarly: true };
    if (year > currentYear - 18) return { yearTooLate: true };

    return null;
  }

  passwordMatchValidator(group: FormGroup) {
    const a = group.get('newPassword')?.value;
    const b = group.get('confirmNewPassword')?.value;
    return a === b ? null : { passwordMismatch: true };
  }

  passwordSameAsCurrentValidator(group: FormGroup) {
    const current = group.get('currentPassword')?.value;
    const next = group.get('newPassword')?.value;

    if (!current || !next) return null;
    return current === next ? { samePassword: true } : null;
  }

  getInitials(): string {
    const f = this.profile?.firstName ?? '';
    const l = this.profile?.lastName ?? '';
    if (!f && !l) return '?';
    return ((f.charAt(0) || '') + (l.charAt(0) || '')).toUpperCase() || '?';
  }

  getFullName(): string {
    const f = this.profile?.firstName ?? '';
    const l = this.profile?.lastName ?? '';
    return `${f} ${l}`.trim() || 'Admin';
  }

  loadProfile(): void {
    this.isLoading = true;
    this.adminService.getMyProfile().subscribe({
      next: (res) => {
        const p = res?.data;
        if (!p) {
          this.isLoading = false;
          this.toastService.showError('Failed to load profile.');
          return;
        }
        this.profile = p;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastService.showError('Failed to load profile.');
      }
    });
  }

  // ============================================================
  // VIEW MODE SWITCHING
  // ============================================================
  showDetails(): void {
    this.viewMode = 'details';
  }

  openEditForm(): void {
    const p = this.profile;
    const dob = p.dateOfBirth ? String(p.dateOfBirth).substring(0, 10) : '';
    this.profileForm.patchValue({
      firstName: p.firstName ?? '',
      lastName: p.lastName ?? '',
      email: p.email ?? '',
      phoneNumber: p.phoneNumber ?? '',
      presentAddress: p.presentAddress ?? '',
      dateOfBirth: dob,
    }, { emitEvent: false });
    this.viewMode = 'edit';
  }

  openPasswordForm(): void {
    this.passwordForm.reset();
    this.viewMode = 'password';
  }

  // ============================================================
  // SAVE PROFILE
  // ============================================================
  onSaveProfile(): void {
    if (this.profileForm.invalid) {
      this.profileForm.markAllAsTouched();
      // this.toastService.showError('Please fill all fields correctly.');
      return;
    }

    const v = this.profileForm.value;
    const payload: UpdateAdminProfileDto = {
      firstName: v.firstName.trim(),
      lastName: v.lastName.trim(),
      email: v.email.trim(),
      phoneNumber: v.phoneNumber.trim(),
      presentAddress: v.presentAddress?.trim() || null,
      dateOfBirth: v.dateOfBirth || null,
    };

    this.isSaving = true;
    this.adminService.updateMyProfile(payload).subscribe({
      next: (res) => {
        this.isSaving = false;
        this.toastService.showSuccess(res.message);

        if (res.data?.requiresRelogin) {
          this.tokenService.clearTokens();
          setTimeout(() => this.router.navigate([this.authService.getLoginUrl()]), 1500);
        } else {
          this.loadProfile();
          this.viewMode = 'details';
        }
      },
      error: () => { this.isSaving = false; }
    });
  }

  // ============================================================
  // CHANGE PASSWORD
  // ============================================================
  onChangePassword(): void {
    if (this.passwordForm.invalid) {
      this.passwordForm.markAllAsTouched();
      // this.toastService.showError('Please fill all password fields correctly.');
      return;
    }

    const v = this.passwordForm.value;
    const payload: ChangePasswordDto = {
      currentPassword: v.currentPassword,
      newPassword: v.newPassword,
      confirmNewPassword: v.confirmNewPassword,
    };

    this.isChangingPassword = true;
    this.adminService.changePassword(payload).subscribe({
      next: (res) => {
        this.isChangingPassword = false;
        this.toastService.showSuccess(res.message);
        this.passwordForm.reset();

        if (res.data?.requiresRelogin) {
          this.tokenService.clearTokens();
          setTimeout(() => this.router.navigate([this.authService.getLoginUrl()]), 1500);
        } else {
          this.viewMode = 'details';
        }
      },
      error: () => { this.isChangingPassword = false; }
    });
  }

  toggleCurrentPassword(): void {
    this.showCurrentPassword = !this.showCurrentPassword;
  }

  toggleNewPassword(): void {
    this.showNewPassword = !this.showNewPassword;
  }

  toggleConfirmPassword(): void {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  onProfilePhoneInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const value = input.value.replace(/\D/g, '').substring(0, 10);
    input.value = value;
    this.profileForm.get('phoneNumber')?.setValue(value, { emitEvent: false });
  }


  // For profile form
  isProfileInvalid(field: string): boolean {
    const c = this.profileForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  getProfileError(field: string): string {
    const c = this.profileForm.get(field);
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
    if (field === 'presentAddress') {
      if (e['maxlength']) return 'Address cannot exceed 500 characters.';
    }
    if (field === 'dateOfBirth') {
      if (e['invalidDate']) return 'Please enter a valid date.';
      if (e['yearTooEarly']) return `Year must be ${new Date().getFullYear() - 100} or later.`;
      if (e['yearTooLate']) return `You must be at least 18 years old.`;
    }

    return '';
  }

  // For password form
  isPasswordInvalid(field: string): boolean {
    const c = this.passwordForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  getPasswordError(field: string): string {
    const c = this.passwordForm.get(field);
    if (!c || !c.errors) return '';
    const e = c.errors;

    if (field === 'currentPassword') {
      if (e['required']) return 'Current password is required.';
    }
    // NAYA
    if (field === 'newPassword') {
      if (e['required']) return 'New password is required.';
      if (e['minlength']) return 'Password must be at least 8 characters long.';
      if (e['maxlength']) return 'Password cannot exceed 100 characters.';
    }
    // method ke return se theek pehle ye add karo (group-level error hai, field-level nahi):
    if (field === 'newPassword' && this.passwordForm.errors?.['samePassword'] && c?.touched) {
      return 'New password must be different from your current password.';
    }
    if (field === 'confirmNewPassword') {
      if (e['required']) return 'Please confirm your new password.';
    }

    return '';
  }
}