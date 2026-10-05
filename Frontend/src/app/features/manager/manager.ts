import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AdminService } from '../../core/services/admin';
import { ToastService } from '../../core/services/toast';
import { ConfirmDialog } from '../../shared/components/confirm-dialog/confirm-dialog';
import { EmployeeService } from '../../core/services/employee';

@Component({
  selector: 'app-manager',
  imports: [CommonModule, ConfirmDialog],
  standalone: true,
  templateUrl: './manager.html',
  styleUrl: './manager.css',
})
export class Manager implements OnInit {
  managers: any[] = [];
  isLoading = false;
  processingUserId: number | null = null;

  dialogOpen = false;
  dialogMessage = '';
  dialogBoldText = '';
  selectedUserId: number | null = null;

  constructor(
    private employeeService: EmployeeService,
    private adminService: AdminService,
    private toastService: ToastService,
  ) {}

  ngOnInit(): void {
    this.loadManagers();
  }

  loadManagers(): void {
    this.isLoading = true;
    this.employeeService.getAll(1, 500).subscribe({
      next: (res: any) => {
        const all = res.data?.items ?? [];
        this.managers = all.filter((e: any) => e.role === 'Manager');
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastService.showError('Failed to load managers. Please refresh the page.');
      }
    });
  }

  openDemoteDialog(emp: any): void {
    this.selectedUserId = emp.userId;
    this.dialogMessage = `Are you sure you want to remove ${emp.fullName} from Manager role? They will need to sign in again.`;
    this.dialogBoldText = emp.fullName;
    this.dialogOpen = true;
  }

  onDialogConfirm(): void {
    if (this.selectedUserId === null) return;
    const userId = this.selectedUserId;
    this.dialogOpen = false;
    this.processingUserId = userId;

    this.adminService.setManagerRole(userId, false).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message);
        this.processingUserId = null;
        this.selectedUserId = null;
        this.loadManagers();
      },
      error: () => { this.processingUserId = null; }
    });
  }

  onDialogCancel(): void {
    this.dialogOpen = false;
    this.selectedUserId = null;
  }
}