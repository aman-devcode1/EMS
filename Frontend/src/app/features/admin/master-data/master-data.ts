import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { ToastService } from '../../../core/services/toast';
import {
  DepartmentDto,
  DesignationDto,
} from '../../../core/models/master-data.model';
import { MasterDataService } from '../../../core/services/master-data'; // adjust path if needed
import { finalize } from 'rxjs';

interface ApiResponse<T> {
  data?: T;
  message: string;
  success?: boolean;
}

@Component({
  selector: 'app-master-data',
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  standalone: true,
  templateUrl: './master-data.html',
  styleUrl: './master-data.css',
})
export class MasterData implements OnInit {
  departments: DepartmentDto[] = [];
  designations: DesignationDto[] = [];

  isLoadingDepartments = false;
  isLoadingDesignations = false;

  // Add form
  departmentForm: FormGroup;
  designationForm: FormGroup;

  isAddingDepartment = false;
  isAddingDesignation = false;

  // Edit state
  editingDepartmentId: number | null = null;
  editingDesignationId: number | null = null;

  editDepartmentForm: FormGroup;
  editDesignationForm: FormGroup;

  isSavingEdit = false;
  processingToggleId: number | null = null;

  showInactive = false;

  constructor(
    private fb: FormBuilder,
    private masterDataService: MasterDataService,
    private toastService: ToastService,
  ) {
    this.departmentForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
    });

    this.designationForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
    });

    this.editDepartmentForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
    });

    this.editDesignationForm = this.fb.group({
      name: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', [Validators.maxLength(500)]],
    });
  }

  ngOnInit(): void {
    this.loadDepartments();
    this.loadDesignations();
  }

  // ============================================================
  // LOAD
  // ============================================================
  loadDepartments(): void {
    this.isLoadingDepartments = true;
    this.masterDataService
      .getDepartments(this.showInactive)
      .pipe(finalize(() => (this.isLoadingDepartments = false)))
      .subscribe({
        next: (res: ApiResponse<DepartmentDto[]>) => {
          this.departments = res.data ?? [];
        },
      });
  }

  loadDesignations(): void {
    this.isLoadingDesignations = true;
    this.masterDataService
      .getDesignations(this.showInactive)
      .pipe(finalize(() => (this.isLoadingDesignations = false)))
      .subscribe({
        next: (res: ApiResponse<DesignationDto[]>) => {
          this.designations = res.data ?? [];
        },
      });
  }

  onShowInactiveChange(value: boolean): void {
    this.showInactive = value;
    this.loadDepartments();
    this.loadDesignations();
  }

  // ============================================================
  // ADD
  // ============================================================
  addDepartment(): void {
    if (this.departmentForm.invalid) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    const v = this.departmentForm.value;
    this.isAddingDepartment = true;
    this.masterDataService
      .createDepartment({
        name: v.name.trim(),
        description: v.description?.trim() || null,
      })
      .pipe(finalize(() => (this.isAddingDepartment = false)))
      .subscribe({
        next: (res: ApiResponse<DepartmentDto>) => {
          this.toastService.showSuccess(res.message);
          this.departmentForm.reset();
          this.loadDepartments();
        },
      });
  }

  addDesignation(): void {
    if (this.designationForm.invalid) {
      this.designationForm.markAllAsTouched();
      return;
    }

    const v = this.designationForm.value;
    this.isAddingDesignation = true;
    this.masterDataService
      .createDesignation({
        name: v.name.trim(),
        description: v.description?.trim() || null,
      })
      .pipe(finalize(() => (this.isAddingDesignation = false)))
      .subscribe({
        next: (res: ApiResponse<DesignationDto>) => {
          this.toastService.showSuccess(res.message);
          this.designationForm.reset();
          this.loadDesignations();
        },
      });
  }

  // ============================================================
  // EDIT
  // ============================================================
  startEditDepartment(d: DepartmentDto): void {
    this.editingDepartmentId = d.id;
    this.editDepartmentForm.patchValue({
      name: d.name,
      description: d.description ?? '',
    });
  }

  cancelEditDepartment(): void {
    this.editingDepartmentId = null;
    this.editDepartmentForm.reset();
  }

  saveEditDepartment(): void {
    if (this.editDepartmentForm.invalid || this.editingDepartmentId === null) {
      this.editDepartmentForm.markAllAsTouched();
      return;
    }

    const v = this.editDepartmentForm.value;
    this.isSavingEdit = true;
    this.masterDataService
      .updateDepartment(this.editingDepartmentId, {
        name: v.name.trim(),
        description: v.description?.trim() || null,
      })
      .pipe(finalize(() => (this.isSavingEdit = false)))
      .subscribe({
        next: (res: ApiResponse<DepartmentDto>) => {
          this.toastService.showSuccess(res.message);
          this.editingDepartmentId = null;
          this.loadDepartments();
        },
      });
  }

  startEditDesignation(d: DesignationDto): void {
    this.editingDesignationId = d.id;
    this.editDesignationForm.patchValue({
      name: d.name,
      description: d.description ?? '',
    });
  }

  cancelEditDesignation(): void {
    this.editingDesignationId = null;
    this.editDesignationForm.reset();
  }

  saveEditDesignation(): void {
    if (this.editDesignationForm.invalid || this.editingDesignationId === null) {
      this.editDesignationForm.markAllAsTouched();
      return;
    }

    const v = this.editDesignationForm.value;
    this.isSavingEdit = true;
    this.masterDataService
      .updateDesignation(this.editingDesignationId, {
        name: v.name.trim(),
        description: v.description?.trim() || null,
      })
      .pipe(finalize(() => (this.isSavingEdit = false)))
      .subscribe({
        next: (res: ApiResponse<DesignationDto>) => {
          this.toastService.showSuccess(res.message);
          this.editingDesignationId = null;
          this.loadDesignations();
        },
      });
  }

  // ============================================================
  // TOGGLE ACTIVE
  // ============================================================
  toggleDepartment(d: DepartmentDto): void {
    this.processingToggleId = d.id;
    this.masterDataService
      .toggleDepartment(d.id)
      .pipe(finalize(() => (this.processingToggleId = null)))
      .subscribe({
        next: (res: ApiResponse<DepartmentDto>) => {
          this.toastService.showSuccess(res.message);
          this.loadDepartments();
        },
      });
  }

  toggleDesignation(d: DesignationDto): void {
    this.processingToggleId = d.id;
    this.masterDataService
      .toggleDesignation(d.id)
      .pipe(finalize(() => (this.processingToggleId = null)))
      .subscribe({
        next: (res: ApiResponse<DesignationDto>) => {
          this.toastService.showSuccess(res.message);
          this.loadDesignations();
        },
      });
  }

  // ============================================================
  // HELPERS
  // ============================================================
  isDepartmentInvalid(field: string): boolean {
    const c = this.departmentForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  isDesignationInvalid(field: string): boolean {
    const c = this.designationForm.get(field);
    return !!(c && c.invalid && c.touched);
  }
}