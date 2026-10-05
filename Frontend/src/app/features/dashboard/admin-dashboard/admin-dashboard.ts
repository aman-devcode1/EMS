import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TokenService } from '../../../core/services/token';
import { CommonModule } from '@angular/common';
import { ToastService } from '../../../core/services/toast';
import { EmployeeService } from '../../../core/services/employee';

@Component({
  selector: 'app-admin-dashboard',
  imports: [RouterLink, CommonModule],
  standalone: true,
  templateUrl: './admin-dashboard.html',
  styleUrl: './admin-dashboard.css',
})
export class AdminDashboard implements OnInit {
  adminName = 'Admin';
  totalEmployees = 0;
  totalManagers = 0;
  isLoadingStats = true;
  statsLoadFailed = false;

  constructor(
    private tokenService: TokenService,
    private employeeService: EmployeeService,
    private toastService: ToastService,
  ) { }

  ngOnInit(): void {
    const name = this.tokenService.getUserName();
    this.adminName = (name && name.trim()) ? name : 'Admin';
    this.loadStats();
  }

  loadStats(): void {
    this.isLoadingStats = true;
    this.statsLoadFailed = false;
    this.employeeService.getAll(1, 500).subscribe({
      next: (result: any) => {
        const all = result.data?.items ?? [];
        this.totalEmployees = all.filter((e: any) => e.role === 'Employee').length;
        this.totalManagers = all.filter((e: any) => e.role === 'Manager').length;
        this.isLoadingStats = false;
      },
      error: () => {
        this.isLoadingStats = false;
        this.statsLoadFailed = true;
        this.toastService.showError('Could not load dashboard stats. Please refresh the page.');
      }
    });
  }
}