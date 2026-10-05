import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EmployeeService } from '../../../core/services/employee';
import { TokenService } from '../../../core/services/token';
import { ToastService } from '../../../core/services/toast';
import { AuthService } from '../../../core/services/auth';

@Component({
  selector: 'app-employee-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink],
  templateUrl: './employee-login.html',
  styleUrl: './employee-login.css'
})
export class EmployeeLogin {
  loginForm: FormGroup;
  isLoading = false;

  // Password visibility
  showPassword = false;

  // Random field names — browser suggestions block karne ke liye
  empEmailFieldName = `f_${Math.random().toString(36).substring(2)}`;
  empPasswordFieldName = `f_${Math.random().toString(36).substring(2)}`;
  empPhoneFieldName = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private fb: FormBuilder,
    private employeeService: EmployeeService,
    private authService: AuthService,
    private toastService: ToastService,
    private tokenService: TokenService,
    private router: Router,
    private route: ActivatedRoute
  ) {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      password: ['', [Validators.required, Validators.minLength(8), Validators.maxLength(100)]],
      phoneNumber: ['', [Validators.required, Validators.pattern('^[6-9]\\d{9}$')]]
    });
  }

  ngOnInit(): void {
  this.route.queryParams.subscribe((params: any) => {
    if (params['reason'] === 'session-expired') {
      this.toastService.showInfo(
        'Your session expired. Please sign in again.',
      );
    }
  });
}

  isInvalid(field: string): boolean {
    const c = this.loginForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  getError(field: string): string {
    const c = this.loginForm.get(field);
    if (!c || !c.errors) return '';
    const e = c.errors;

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
    if (field === 'phoneNumber') {
      if (e['required']) return 'Phone number is required.';
      if (e['pattern']) return 'Enter a valid 10-digit Indian phone number starting with 6-9.';
    }
    return '';
  }

  togglePassword() {
    this.showPassword = !this.showPassword;
  }

  onPhoneInput(event: Event) {
    const input = event.target as HTMLInputElement;

    // Sirf numbers allow karo
    let value = input.value.replace(/\D/g, '');

    // Maximum 10 digits
    value = value.substring(0, 10);

    input.value = value;

    this.loginForm.get('phoneNumber')?.setValue(value, {
      emitEvent: false
    });
  }

  onSubmit() {
    // (Frontend ka custom error nahi, HTML validators dikhayenge)
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;

    // Direct token return hoga, OTP nahi
    this.employeeService.loginEmployee(this.loginForm.value).subscribe({
      next: (response) => {
        this.isLoading = false;
        this.toastService.showSuccess(response.message); // Backend ka message

        // Tokens save karo
        this.authService.saveSession({
          accessToken: response.data.accessToken,
        });

        // Redirect to Employee Dashboard
        this.router.navigate(['/employee/dashboard']);
      },
      error: () => {
        this.isLoading = false;
        // Error Interceptor khud handle karega
      }
    });
  }
}