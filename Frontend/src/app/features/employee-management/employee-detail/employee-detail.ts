import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ToastService } from '../../../core/services/toast';
import { EmployeeResponseDto, UpdateEmployeeProfessionalDto } from '../../../core/models/employee.model';
import { DepartmentDto, DesignationDto } from '../../../core/models/master-data.model';
import { finalize } from 'rxjs';
import { EmployeeService } from '../../../core/services/employee';
import { MasterDataService } from '../../../core/services/master-data';

type ViewMode = 'details' | 'edit';

@Component({
  selector: 'app-employee-detail',
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  standalone: true,
  templateUrl: './employee-detail.html',
  styleUrl: './employee-detail.css',
})
export class EmployeeDetail implements OnInit {
  employee?: EmployeeResponseDto;
  departments: DepartmentDto[] = [];
  designations: DesignationDto[] = [];

  isLoading = true;
  loadFailed = false;
  isSaving = false;
  viewMode: ViewMode = 'details';

  editForm: FormGroup;

  // Random field names (browser autocomplete block)
  fDept = `f_${Math.random().toString(36).substring(2)}`;
  fDesig = `f_${Math.random().toString(36).substring(2)}`;
  fSalary = `f_${Math.random().toString(36).substring(2)}`;
  fPrevRole = `f_${Math.random().toString(36).substring(2)}`;

  constructor(
    private route: ActivatedRoute,
    private fb: FormBuilder,
    private employeeService: EmployeeService,
    private masterDataService: MasterDataService,
    private toastService: ToastService,
  ) {
    this.editForm = this.fb.group({
      departmentId: [null],
      designationId: [null],
      salary: [null, [Validators.min(10000)]],
      previousCompanyRole: ['', [Validators.maxLength(200)]],
    });
  }

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!id) {
      this.loadFailed = true;
      this.isLoading = false;
      return;
    }
    this.loadMasterData();
    this.loadEmployee(id);
  }

  // ============================================================
  // LOAD
  // ============================================================
  loadEmployee(id: number): void {
    this.isLoading = true;
    this.loadFailed = false;

    this.employeeService.getById(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.employee = res.data;
        } else {
          this.loadFailed = true;
          this.toastService.showError(res.message || 'Employee not found.');
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.loadFailed = true;
        this.toastService.showError('Unable to load employee details.');
      },
    });
  }

  loadMasterData(): void {
    if (this.departments.length === 0) {
      this.masterDataService.getDepartments().subscribe({
        next: (res) => (this.departments = res.data ?? []),
      });
    }
    if (this.designations.length === 0) {
      this.masterDataService.getDesignations().subscribe({
        next: (res) => (this.designations = res.data ?? []),
      });
    }
  }

  // ============================================================
  // EDIT MODE
  // ============================================================
  openEditForm(): void {
    if (!this.employee) return;

    this.loadMasterData();

    this.editForm.patchValue({
      departmentId: this.employee.departmentId ?? null,
      designationId: this.employee.designationId ?? null,
      salary: this.employee.salary && this.employee.salary > 0 ? this.employee.salary : null,
      previousCompanyRole: this.employee.previousCompanyRole ?? '',
    });

    this.viewMode = 'edit';
  }

  cancelEdit(): void {
    this.editForm.reset();
    this.viewMode = 'details';
  }

  // ============================================================
  // SAVE
  // ============================================================
  onSaveProfessional(): void {
    if (!this.employee) return;

    if (this.editForm.invalid) {
      this.editForm.markAllAsTouched();
      return;
    }

    const v = this.editForm.value;
    const payload: UpdateEmployeeProfessionalDto = {
      departmentId: v.departmentId ?? null,
      designationId: v.designationId ?? null,
      salary: v.salary != null && v.salary !== '' ? Number(v.salary) : null,
      previousCompanyRole: v.previousCompanyRole?.trim() || null,
    };

    this.isSaving = true;
    this.employeeService
      .updateProfessionalInfo(this.employee.id, payload)
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.employee = res.data;
          this.viewMode = 'details';
        },
      });
  }

  // ============================================================
  // HELPERS
  // ============================================================
  getInitials(): string {
    const name = this.employee?.fullName ?? '';
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  isInvalid(field: string): boolean {
    const c = this.editForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  getError(field: string): string {
    const c = this.editForm.get(field);
    if (!c || !c.errors) return '';
    const e = c.errors;

    if (field === 'salary') {
      if (e['min']) return 'Salary must be at least Rs.10,000.';
    }
    if (field === 'previousCompanyRole') {
      if (e['maxlength']) return 'Previous company role cannot exceed 200 characters.';
    }
    return '';
  }

  getDepartmentName(): string {
    if (this.employee?.department) return this.employee.department;
    const id = this.employee?.departmentId;
    if (!id) return '';
    return this.departments.find(d => d.id === id)?.name ?? '';
  }

  getDesignationName(): string {
    if (this.employee?.designation) return this.employee.designation;
    const id = this.employee?.designationId;
    if (!id) return '';
    return this.designations.find(d => d.id === id)?.name ?? '';
  }
}