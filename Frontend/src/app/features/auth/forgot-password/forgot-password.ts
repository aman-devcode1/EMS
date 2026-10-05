import { Component } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../../core/services/auth';
import { ToastService } from '../../../core/services/toast';
import { Router, RouterLink } from '@angular/router';
import { CommonModule } from '@angular/common';
import { finalize } from 'rxjs';

@Component({
  selector: 'app-forgot-password',
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLink],
  standalone: true,
  templateUrl: './forgot-password.html',
  styleUrl: './forgot-password.css',
})
export class ForgotPassword {
  forgotForm: FormGroup;
  isLoading = false;
  // Submit hone ke baad form ki jagah ek neutral confirmation dikhate hain —
  // backend jaanbujh kar generic message deta hai (email exist karta hai ya
  // nahi ye reveal nahi karna), isliye UI bhi wahi treat karta hai.
  isSubmitted = false;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastService: ToastService,
    private router: Router,
  ) {
    this.forgotForm = this.fb.group({
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
    });
  }

  onSubmit() {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    const email = this.forgotForm.value.email;

    this.authService.forgotPassword({ email })
    .pipe(finalize(() => this.isLoading = false))
    .subscribe({
      next: (response) => {
        this.isSubmitted = true;
        // backend ka genermic message ghi dikhate hai - koi extra info add nhi karte hai.
      },
      error: () => {
        this.isLoading = false;
        this.toastService.showError("Something went wrong. Please try again.");
      },
    });
  }

  goToResetPassword() {
    this.router.navigate(['/reset-password'], {
      queryParams: { email: this.forgotForm.value.email },
    });
  }
}