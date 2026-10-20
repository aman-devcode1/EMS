import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast';
import { EmployeeResponseDto } from '../../../core/models/employee.model';
import { EmployeeService } from '../../../core/services/employee';

@Component({
  selector: 'app-employee-dashboard',
  imports: [CommonModule, RouterLink],
  standalone: true,
  templateUrl: './employee-dashboard.html',
  styleUrl: './employee-dashboard.css',
})
export class EmployeeDashboard implements OnInit {
  employee?: EmployeeResponseDto;
  greeting = 'Welcome Back';
  isLoading = true;
  loadFailed = false;

  constructor(
    private employeeService: EmployeeService,
    private toastService: ToastService,
  ) { }

  ngOnInit(): void {
    if (sessionStorage.getItem('isFirstLogin') === 'true') {
      this.greeting = 'Welcome';
      sessionStorage.removeItem('isFirstLogin');
    }

    this.loadProfile();
  }

  loadProfile(): void {
    this.isLoading = true;
    this.loadFailed = false;
    this.employeeService.getMyProfile().subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.employee = res.data;
        } else {
          this.loadFailed = true;
          this.toastService.showError(res.message || 'Profile data not found.');
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.loadFailed = true;
        this.toastService.showError('Unable to load profile. Please try again.');
      },
    });
  }
}