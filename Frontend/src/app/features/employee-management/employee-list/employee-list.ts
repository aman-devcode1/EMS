import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AdminService } from '../../../core/services/admin';
import { ToastService } from '../../../core/services/toast';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';
import { EmployeeService } from '../../../core/services/employee';
import { TokenService } from '../../../core/services/token';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-employee-list',
  imports: [CommonModule, ConfirmDialog, DatePipe, FormsModule],
  standalone: true,
  templateUrl: './employee-list.html',
  styleUrl: './employee-list.css',
})
export class EmployeeList implements OnInit {
  employees: any[] = [];
  allEmployees: any[] = [];
  managerExists = false;
  isLoading = false;
  isAdmin = false;
  processingUserId: number | null = null;

  currentView: 'active' | 'deactivated' = 'active'

  searchTerm: string = '';

  // Dialog State
  dialogOpen = false;
  dialogTitle = '';
  dialogMessage = '';
  dialogConfirmText = '';
  dialogBoldText = '';
  dialogVariant: 'primary' | 'danger' = 'primary';

  selectedUserId: number | null = null;
  selectedAction: 'promote' | 'demote' = 'promote';

  constructor(
    private employeeService: EmployeeService,
    private adminService: AdminService,
    private toastService: ToastService,
    private tokenService: TokenService,
    private router: Router,
  ) { }

  ngOnInit(): void {
    this.isAdmin = this.tokenService.isAdmin();
    this.loadEmployees();
  }

  loadEmployees(): void {
    this.isLoading = true;
    this.employeeService.getAll(1, 500).subscribe({
      next: (res: any) => {
        const allRecords = res.data?.items ?? [];
        
        this.managerExists = allRecords.some((e: any) => e.role === 'Manager');

        this.allEmployees = allRecords.filter((e: any) => {
          const isEmployee = e.role === 'Employee';
          const matchsTab = this.currentView === 'active' ? e.isActive : !e.isActive;
          return isEmployee && matchsTab;
      });
        this.isLoading = false;

        this.applyFilter();
      },
      error: () => {
        this.isLoading = false;
        this.toastService.showError('Failed to load employees. Please refresh the page.');
      }
    });
  }

  setView(view: 'active' | 'deactivated'): void {
    this.currentView = view;
    this.searchTerm = '';
    this.loadEmployees(); // List reload karo
  }

  canToggle(emp: any): boolean {
    if (!this.isAdmin) return false;
    if (emp.role === 'Manager') return true;
    return !this.managerExists;
  }

  // 🔥 Row click → detail page
  openDetail(emp: any): void {
    this.router.navigate(['/admin/employees', emp.id]);
  }

  openPromoteDialog(emp: any, event: Event): void {
    event.stopPropagation();

    if (!this.canToggle(emp)) return;

    this.selectedUserId = emp.userId;

    // Agar pehle se manager hai, toh 'Demote' ka dialog khulega
    if (emp.role === 'Manager') {
      this.selectedAction = 'demote';
      this.dialogTitle = 'Remove from Manager';
      this.dialogMessage = `Are you sure you want to remove ${emp.fullName} from Manager? They will need to sign in again.`;
      this.dialogConfirmText = 'Demote';
      this.dialogVariant = 'danger';
    } else {
      this.selectedAction = 'promote';
      this.dialogTitle = 'Promote to Manager';
      this.dialogMessage = `Are you sure you want to promote ${emp.fullName} to Manager? They will need to sign in again.`;
      this.dialogConfirmText = 'Promote';
      this.dialogVariant = 'primary';
    }

    this.dialogBoldText = emp.fullName;
    this.dialogOpen = true;
  }

  onDialogConfirm(): void {
    if (this.selectedUserId === null) return;

    const userId = this.selectedUserId;
    const isManagerAction = this.selectedAction === 'promote';

    this.dialogOpen = false;
    this.processingUserId = userId;

    // true = Promote, false = Demote
    this.adminService.setManagerRole(userId, isManagerAction).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message);
        this.processingUserId = null;
        this.selectedUserId = null;
        this.loadEmployees(); // List refresh karo (isse managerExists bhi update ho jayega)
      },
      error: () => {
        this.processingUserId = null;
      }
    });
  }

  onDialogCancel(): void {
    this.dialogOpen = false;
    this.selectedUserId = null;
  }

  // ============================================================
  // APPLY FILTER (Search logic)
  // ============================================================
  applyFilter(): void {
    if (!this.searchTerm || this.searchTerm.trim() === '') {
      this.employees = this.allEmployees;
      return;
    }

    const term = this.searchTerm.toLowerCase().trim();

    this.employees = this.allEmployees.filter(e => {
      return (
        (e.fullName && e.fullName.toLowerCase().includes(term)) ||
        (e.email && e.email.toLowerCase().includes(term)) ||
        (e.phoneNumber && e.phoneNumber.includes(term)) ||
        (e.department && e.department.toLowerCase().includes(term)) ||
        (e.designation && e.designation.toLowerCase().includes(term)) ||
        (e.role && e.role.toLowerCase().includes(term))
      );
    });
  }
}