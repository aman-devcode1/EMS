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
  selector: 'app-manager-detail',
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  standalone: true,
  templateUrl: './manager-detail.html',
  styleUrl: './manager-detail.css',
})
export class ManagerDetail implements OnInit {
  manager?: EmployeeResponseDto;
  departments: DepartmentDto[] = [];
  designations: DesignationDto[] = [];

  isLoading = true;
  loadFailed = false;
  isSaving = false;
  viewMode: ViewMode = 'details';

  editForm: FormGroup;

  // Random field names
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
    this.loadManager(id);
  }

  loadManager(id: number): void {
    this.isLoading = true;
    this.loadFailed = false;

    this.employeeService.getById(id).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.manager = res.data;
        } else {
          this.loadFailed = true;
          this.toastService.showError(res.message || 'Manager not found.');
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.loadFailed = true;
        this.toastService.showError('Unable to load manager details.');
      },
    });
  }

  loadMasterData(): void {
    this.masterDataService.getAllMasterData().subscribe({
      next: (res: any) => {
        this.departments = res.data?.departments ?? [];
        this.designations = res.data?.designations ?? [];
      },
      error: () => {
        this.toastService.showError('Failed to load dropdown data.');
      }
    });
  }

  openEditForm(): void {
    if (!this.manager) return;

    this.loadMasterData();

    this.editForm.patchValue({
      departmentId: this.manager.departmentId ?? null,
      designationId: this.manager.designationId ?? null,
      salary: this.manager.salary && this.manager.salary > 0 ? this.manager.salary : null,
      previousCompanyRole: this.manager.previousCompanyRole ?? '',
    });

    this.viewMode = 'edit';
  }

  cancelEdit(): void {
    this.editForm.reset();
    this.viewMode = 'details';
  }

  onSaveProfessional(): void {
    if (!this.manager) return;

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
      .updateProfessionalInfo(this.manager.id, payload)
      .pipe(finalize(() => (this.isSaving = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.manager = res.data;
          this.viewMode = 'details';
        },
      });
  }

  getInitials(): string {
    const name = this.manager?.fullName ?? '';
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
    if (this.manager?.department) return this.manager.department;
    const id = this.manager?.departmentId;
    if (!id) return '';
    return this.departments.find(d => d.id === id)?.name ?? '';
  }

  getDesignationName(): string {
    if (this.manager?.designation) return this.manager.designation;
    const id = this.manager?.designationId;
    if (!id) return '';
    return this.designations.find(d => d.id === id)?.name ?? '';
  }
}