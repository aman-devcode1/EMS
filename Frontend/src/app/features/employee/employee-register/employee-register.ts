import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast';
import { validate } from '@angular/forms/signals';
import { EmployeeService } from '../../../core/services/employee';

// Every month's max days (leap year)
const MAX_DAYS = [31, 29, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31];

// Allowed year range: from (currentyYear - 100) to (currentyYear - 18)
function getYearRange() {
  const currentYear = new Date().getFullYear();
  return { min: currentYear - 100, max: currentYear - 18 };
}

// On typing check: ab tak ke digits (jaise "3105") valid prefix check
function isValidDobPrefix(s: string): boolean {
  const n = s.length;

  if (n >= 1 && +s[0] > 3) return false;                       // first digit of day 0-3
  if (n >= 2) {
    const day = +s.slice(0, 2);
    if (day < 1 || day > 31) return false;                     // day 01-31
  }
  if (n >= 3 && +s[2] > 1) return false;                       // first digit of month 0-1
  if (n >= 4) {
    const month = +s.slice(2, 4);
    if (month < 1 || month > 12) return false;                 // month 01-12
    if (+s.slice(0, 2) > MAX_DAYS[month - 1]) return false;    // not the 31-04
  }
  if (n >= 5) {
    const { min, max } = getYearRange();
    const yearPrefix = s.slice(4);
    let ok = false;
    for (let y = min; y <= max; y++) {
      if (String(y).startsWith(yearPrefix)) { ok = true; break; }
    }
    if (!ok) return false;                                     // out of year range
  }
  return true;
}

@Component({
  selector: 'app-employee-register',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './employee-register.html',
  styleUrl: './employee-register.css',
})
export class EmployeeRegister {
  registerForm: FormGroup;
  isLoading = false;

  // Password visibility
  showPassword = false;
  showConfirmPassword = false;

  // Random field names — browser suggestions block karne ke liye
  erFirstNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erLastNameFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erEmailFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erPhoneFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erConfirmPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erDobFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erAddressFieldName = `f_${Math.random().toString(36).substring(2)}`;
  erPrevRoleFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private employeeService: EmployeeService,
    private toastService: ToastService,
    private router: Router,
  ) {
    this.registerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(100)]],
      lastName: ['', [Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      confirmPassword: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.required, Validators.pattern('^[6-9]\\d{9}$')]],
      dateOfBirth: ['', [Validators.required, Validators.pattern(/^\d{2}-\d{2}-\d{4}$/), this.dateRangeValidator]],
      presentAddress: ['', [Validators.maxLength(500)]],
      previousCompanyRole: ['', [Validators.maxLength(200)]],
    },
      {
        validators: this.passwordMatchValidator
      });
  }

  // employee-register validator
  dateRangeValidator(control: any) {
    const value = control.value;
    if (!value) return null;

    // Poora format nahi hai to pattern validator message dega
    if (!/^\d{2}-\d{2}-\d{4}$/.test(value)) return null;

    const [day, month, year] = value.split('-').map(Number);

    if (month < 1 || month > 12) return { invalidMonth: true };
    if (day < 1 || day > 31) return { invalidDay: true };

    // 30-02, 31-04, non-leap year mein 29-02 jaisi dates pakadta hai
    const d = new Date(year, month - 1, day);
    if (d.getMonth() !== month - 1 || d.getDate() !== day) {
      return { invalidDayForMonth: true };
    }

    const { min, max } = getYearRange();
    if (year < min) return { yearTooEarly: true };
    if (year > max) return { yearTooLate: true };

    return null;
  }

  passwordMatchValidator(formGroup: FormGroup) {
    const password = formGroup.get('password')?.value;
    const confirmPassword = formGroup.get('confirmPassword')?.value;

    if (!password || !confirmPassword) {
      return null;
    }

    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  toggleConfirmPassword() {
    this.showConfirmPassword = !this.showConfirmPassword;
  }

  // Only digits in Phone number (no alphabates, spaces, extra didgits)
  onPhoneInput(event: Event) {
    const input = event.target as HTMLInputElement;

    let value = input.value.replace(/\D/g, '');

    value = value.substring(0, 10);

    input.value = value;

    this.registerForm.get('phoneNumber')?.setValue(value, { emitEvent: false });
  }

  onSubmit() {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      // this.toastService.showError('Please fill in the all required feilds correctly.');
      return;
    }

    this.isLoading = true;

    // No need to send Confirm Password to backend.
    const {
      confirmPassword, ...registerData
    } = this.registerForm.value;

    const [day, month, year] = registerData.dateOfBirth.split('-');

    registerData.dateOfBirth = `${year}-${month}-${day}`;

    this.employeeService.registerEmployee(registerData).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastService.showSuccess(response.message);
        this.router.navigate(['/employee/verify-otp'], {
          queryParams: {
            email: this.registerForm.value.email,
            mode: 'register'
          },
        });
      },

      error: () => {
        this.isLoading = false;
      }
    });
  }

  onDobInput(event: Event) {
    const input = event.target as HTMLInputElement;

    // sirf digits, max 8 (DDMMYYYY)
    const raw = input.value.replace(/\D/g, '').substring(0, 8);

    // Sirf wahi digit rakho jo day/month/year ke rule se valid ho
    let digits = '';
    for (const ch of raw) {
      if (isValidDobPrefix(digits + ch)) digits += ch;
    }

    // DD-MM-YYYY format
    let value = digits;
    if (digits.length > 4) {
      value = digits.substring(0, 2) + '-' + digits.substring(2, 4) + '-' + digits.substring(4);
    } else if (digits.length > 2) {
      value = digits.substring(0, 2) + '-' + digits.substring(2);
    }

    input.value = value;
    this.registerForm.get('dateOfBirth')?.setValue(value, { emitEvent: false });
  }

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
    if (field === 'password') {
      if (e['required']) return 'Password is required.';
      if (e['minlength']) return 'Password must be at least 8 characters long.';
      if (e['maxlength']) return 'Password cannot exceed 100 characters.';
    }
    if (field === 'confirmPassword') {
      if (e['required']) return 'Please confirm your password.';
      if (e['minlength']) return 'Confirm password must be at least 8 characters long.';
      if (e['maxlength']) return 'Confirm password cannot exceed 100 characters.';
    }
    if (field === 'phoneNumber') {
      if (e['required']) return 'Phone number is required.';
      if (e['pattern']) return 'Enter a valid 10-digit Indian phone number starting with 6-9.';
    }
    if (field === 'dateOfBirth') {
      const { min, max } = getYearRange();
      if (e['required']) return 'Date of birth is required.';
      if (e['pattern']) return 'Please enter the date in DD-MM-YYYY format.';
      if (e['invalidMonth']) return 'Month must be between 01 and 12.';
      if (e['invalidDay']) return 'Day must be between 01 and 31.';
      if (e['invalidDayForMonth']) return 'This day does not exist in the selected month.';
      if (e['yearTooEarly']) return `Year must be ${min} or later.`;
      if (e['yearTooLate']) return `You must be at least 18 years old (year ${max} or earlier).`;
    }
    if (field === 'presentAddress') {
      if (e['maxlength']) return 'Address cannot exceed 500 characters.';
    }
    if (field === 'previousCompanyRole') {
      if (e['maxlength']) return 'Role cannot exceed 200 characters.';
    }

    return '';
  }
}