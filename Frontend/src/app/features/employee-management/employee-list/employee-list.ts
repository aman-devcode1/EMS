import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { EmployeeService } from '../../../core/services/employee';

@Component({
  selector: 'app-employee-list',
  imports: [CommonModule, ConfirmDialog],
  standalone: true,
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.css',
})
export class EmployeeList implements OnInit {
  employees: any[] = [];
  currentManagerId: number | null = null;
  managerCheckFailed = false;
  isLoading = false;
  processingUserId: number | null = null;

  // Dialog State
  dialogOpen = false;
  dialogTitle = '';
  dialogMessage = '';
  dialogConfirmText = '';
  dialogBoldText = '';
  dialogVariant: 'primary' | 'danger' = 'primary';
  selectedUserId: number | null = null;

  constructor(
    private employeeService: EmployeeService,
    private adminService: AdminService,
    private toastService: ToastService,
    private router: Router,
  ) { }

  ngOnInit(): void {
    this.loadEmployees();
    this.loadManager();
  }

  loadEmployees(): void {
    this.isLoading = true;
    this.employeeService.getAll(1, 500).subscribe({
      next: (res: any) => {
        const all = res.data?.items ?? [];
        this.employees = all.filter((e: any) => e.role === 'Employee');
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastService.showError('Failed to load employees. Please refresh the page.');
      }
    });
  }

  loadManager(): void {
    this.managerCheckFailed = false;
    this.adminService.getManager().subscribe({
      next: (res) => {
        this.currentManagerId = res.data?.managerId ?? null;
      },
      error: () => {
        this.managerCheckFailed = true;
        this.toastService.showError('Could not verify current manager status. Promote is disabled until this is confirmed.');
      }
    });
  }

  canToggle(): boolean {
    if (this.managerCheckFailed) return false;
    return this.currentManagerId === null;
  }

  // 🔥 Row click → detail page
  openDetail(emp: any): void {
    this.router.navigate(['/admin/employees', emp.id]);
  }

  openPromoteDialog(emp: any, event: Event): void {
    // Button click से row click को रोकना
    event.stopPropagation();

    if (!this.canToggle()) return;
    this.selectedUserId = emp.userId;
    this.dialogTitle = 'Promote to Manager';
    this.dialogMessage = `Are you sure you want to promote ${emp.fullName} to Manager? They will need to sign in again.`;
    this.dialogBoldText = emp.fullName;
    this.dialogConfirmText = 'Promote';
    this.dialogVariant = 'primary';
    this.dialogOpen = true;
  }

  onDialogConfirm(): void {
    if (this.selectedUserId === null) return;
    const userId = this.selectedUserId;
    this.dialogOpen = false;
    this.processingUserId = userId;

    this.adminService.setManagerRole(userId, true).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message);
        this.processingUserId = null;
        this.selectedUserId = null;
        this.loadEmployees();
        this.loadManager();
      },
      error: () => { this.processingUserId = null; }
    });
  }

  onDialogCancel(): void {
    this.dialogOpen = false;
    this.selectedUserId = null;
  }
}