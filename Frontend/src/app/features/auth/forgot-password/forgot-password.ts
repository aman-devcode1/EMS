import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/services/auth';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword implements OnInit {
  forgotForm: FormGroup;
  isLoading = false;
  // Backend jaanbujh kar generic message deta hai, isliye UI bhi neutral confirmation dikhata hai.
  isSubmitted = false;

  currentRole: 'admin' | 'employee' | 'manager' = 'admin';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    });
  }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['role']) {
        this.currentRole = params['role'];
      }
    });
  }

  onSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const email = this.forgotForm.value.email;

    this.authService.forgotPassword({ email })
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: () => {
          this.isSubmitted = true;
        },
        // Error toast errorInterceptor dikhata hai. Yahan dobara toast nahi.
        error: () => { },
      });
  }

  goToResetPassword(): void {
    this.router.navigate(['/reset-password'], {
      queryParams: { email: this.forgotForm.value.email },
    });
  }

  // ✅ Helper: Role ke hisaab se login URL
  getLoginUrl(): string {
    if (this.currentRole === 'employee') return '/employee/login';
    return '/admin/login'; // Admin aur Manager dono yahan jayenge
  }

  // ✅ Helper: Role ke hisaab se text
  getRoleLabel(): string {
    if (this.currentRole === 'employee') return 'Employee';
    if (this.currentRole === 'manager') return 'Manager';
    return 'Admin';
  }
}