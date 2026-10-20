import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { EmployeeService } from '../../../core/services/employee';

@Component({
  selector: 'app-manager',
  imports: [CommonModule, ConfirmDialog, DatePipe],
  standalone: true,
  templateUrl: './manager-list.html',
  styleUrl: './manager-list.css',
})
export class ManagerList implements OnInit {
  managers: any[] = [];
  isLoading = false;
  processingUserId: number | null = null;

  // Dialog State
  dialogOpen = false;
  dialogMessage = '';
  dialogBoldText = '';
  selectedUserId: number | null = null;

  constructor(
    private employeeService: EmployeeService,
    private adminService: AdminService,
    private toastService: ToastService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.loadManagers();
  }

  loadManagers(): void {
    this.isLoading = true;
    this.employeeService.getAll(1, 500).subscribe({
      next: (res: any) => {
        const all = res.data?.items ?? [];
        // Sirf Manager ko filter karo
        this.managers = all.filter((e: any) => e.role === 'Manager');
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastService.showError('Failed to load managers. Please refresh the page.');
      }
    });
  }

  // 🔥 Row click -> Employee Detail Page (Yahan Edit button pehle se hai)
  openDetail(m: any): void {
    this.router.navigate(['/admin/managers', m.id]);
  }

  openDemoteDialog(emp: any, event: Event): void {
    event.stopPropagation(); 
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