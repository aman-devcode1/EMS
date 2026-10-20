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
import { DepartmentDto, DesignationDto } from '../../../core/models/master-data.model';
import { MasterDataService } from '../../../core/services/master-data';
import { finalize, forkJoin } from 'rxjs';
import { ConfirmDialog } from '../../../shared/components/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-master-data',
  imports: [CommonModule, ReactiveFormsModule, FormsModule, ConfirmDialog],
  standalone: true,
  templateUrl: './master-data.html',
  styleUrl: './master-data.css',
})
export class MasterData implements OnInit {
  // ✅ Original Data (API se aaya hua)
  allDepartments: DepartmentDto[] = [];
  allDesignations: DesignationDto[] = [];

  // ✅ Filtered Data (UI par dikhne wala)
  departments: DepartmentDto[] = [];
  designations: DesignationDto[] = [];

  isLoading = false;
  currentView: 'all' | 'active' | 'inactive' = 'all';
  searchTerm = ''; // ✅ Search Term

  // Forms
  departmentForm: FormGroup;
  designationForm: FormGroup;
  editDepartmentForm: FormGroup;
  editDesignationForm: FormGroup;

  isAddingDepartment = false;
  isAddingDesignation = false;
  isSavingEdit = false;

  // Selection State
  selectedDeptIds: number[] = [];
  selectedDesigIds: number[] = [];
  isProcessingBulk = false;

  editingDepartmentId: number | null = null;
  editingDesignationId: number | null = null;

  // Dialog State
  dialogOpen = false;
  dialogTitle = '';
  dialogMessage = '';
  dialogBoldText = '';
  dialogConfirmText = 'Delete';
  dialogVariant: 'primary' | 'danger' = 'danger';
  pendingDeleteAction: { type: 'department' | 'designation', id: number } | null = null;

  constructor(
    private fb: FormBuilder,
    private masterDataService: MasterDataService,
    private toastService: ToastService,
  ) {
    // ... form initializations same rahenge ...
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
    this.loadAllData();
  }

  setView(view: 'all' | 'active' | 'inactive'): void {
    this.currentView = view;
    this.clearSelections();
    this.applyFilters(); // API call nahi, sirf local filter
  }

  // ============================================================
  // LOAD DATA (Sirf ek baar API call)
  // ============================================================
  loadAllData(): void {
    this.isLoading = true;
    this.masterDataService.getAllMasterData(true)
      .pipe(finalize(() => (this.isLoading = false)))
      .subscribe({
        next: (res) => {
          // Original data save karo
          this.allDepartments = res.data?.departments ?? [];
          this.allDesignations = res.data?.designations ?? [];

          // UI ke liye filter karo
          this.applyFilters();
        },
        error: () => {
          this.toastService.showError('Failed to load master data.');
        }
      });
  }

  // ============================================================
  // ✅ APPLY FILTERS (Search + Tabs)
  // ============================================================
  applyFilters(): void {
    let filteredDepts = this.allDepartments;
    let filteredDesigs = this.allDesignations;

    // 1. Tab Filter (All / Active / Inactive)
    if (this.currentView === 'active') {
      filteredDepts = filteredDepts.filter(d => d.isActive && d.isAssigned);
      filteredDesigs = filteredDesigs.filter(d => d.isActive && d.isAssigned);
    } else if (this.currentView === 'inactive') {
      filteredDepts = filteredDepts.filter(d => !d.isActive);
      filteredDesigs = filteredDesigs.filter(d => !d.isActive);
    }

    // 2. Search Filter
    if (this.searchTerm && this.searchTerm.trim() !== '') {
      const term = this.searchTerm.toLowerCase().trim();

      filteredDepts = filteredDepts.filter(d =>
        d.name.toLowerCase().includes(term) ||
        (d.description && d.description.toLowerCase().includes(term))
      );

      filteredDesigs = filteredDesigs.filter(d =>
        d.name.toLowerCase().includes(term) ||
        (d.description && d.description.toLowerCase().includes(term))
      );
    }

    // UI arrays ko update karo
    this.departments = filteredDepts;
    this.designations = filteredDesigs;

    // Jo items filter hone ke baad gayab ho gaye, unko selection se hatao
    this.selectedDeptIds = this.selectedDeptIds.filter(id => filteredDepts.some(d => d.id === id));
    this.selectedDesigIds = this.selectedDesigIds.filter(id => filteredDesigs.some(d => d.id === id));
  }

  // ============================================================
  // SELECTION LOGIC
  // ============================================================
  toggleDeptSelection(id: number): void {
    const idx = this.selectedDeptIds.indexOf(id);
    if (idx > -1) this.selectedDeptIds.splice(idx, 1);
    else this.selectedDeptIds.push(id);
  }

  toggleDesigSelection(id: number): void {
    const idx = this.selectedDesigIds.indexOf(id);
    if (idx > -1) this.selectedDesigIds.splice(idx, 1);
    else this.selectedDesigIds.push(id);
  }

  isDeptSelected(id: number): boolean {
    return this.selectedDeptIds.includes(id);
  }

  isDesigSelected(id: number): boolean {
    return this.selectedDesigIds.includes(id);
  }

  clearSelections(): void {
    this.selectedDeptIds = [];
    this.selectedDesigIds = [];
  }

  toggleAllDepartments(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedDeptIds = checked ? this.departments.map(d => d.id) : [];
  }

  isAllDeptsSelected(): boolean {
    return this.departments.length > 0 && this.selectedDeptIds.length === this.departments.length;
  }

  toggleAllDesignations(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.selectedDesigIds = checked ? this.designations.map(d => d.id) : [];
  }

  isAllDesigsSelected(): boolean {
    return this.designations.length > 0 && this.selectedDesigIds.length === this.designations.length;
  }

  // ============================================================
  // BULK ACTION
  // ============================================================
  processBulkAction(type: 'department' | 'designation'): void {
    const ids = type === 'department' ? this.selectedDeptIds : this.selectedDesigIds;
    if (ids.length === 0) return;

    this.isProcessingBulk = true;

    const apiCalls = ids.map(id =>
      type === 'department'
        ? this.masterDataService.toggleDepartment(id)
        : this.masterDataService.toggleDesignation(id)
    );

    forkJoin(apiCalls).subscribe({
      next: () => {
        this.toastService.showSuccess(`Selected ${type}s updated successfully.`);
        this.isProcessingBulk = false;
        this.clearSelections();
        this.loadAllData(); // Refresh list
      },
      error: () => {
        this.toastService.showError(`Failed to update some ${type}s.`);
        this.isProcessingBulk = false;
        this.loadAllData();
      }
    });
  }

  // ============================================================
  // ADD / EDIT / DELETE (Baaki methods same rahenge)
  // ============================================================
  // ... (Copy the same methods from your previous file: addDepartment, addDesignation, startEdit, saveEdit, openDeleteDialog, onDialogConfirm, onDialogCancel, isDepartmentInvalid, isDesignationInvalid) ...

  addDepartment(): void {
    if (this.departmentForm.invalid) { this.departmentForm.markAllAsTouched(); return; }
    const v = this.departmentForm.value;
    this.isAddingDepartment = true;
    this.masterDataService.createDepartment({ name: v.name.trim(), description: v.description?.trim() || null })
      .pipe(finalize(() => (this.isAddingDepartment = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.departmentForm.reset();
          this.loadAllData();
        },
      });
  }

  addDesignation(): void {
    if (this.designationForm.invalid) { this.designationForm.markAllAsTouched(); return; }
    const v = this.designationForm.value;
    this.isAddingDesignation = true;
    this.masterDataService.createDesignation({ name: v.name.trim(), description: v.description?.trim() || null })
      .pipe(finalize(() => (this.isAddingDesignation = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.designationForm.reset();
          this.loadAllData();
        },
      });
  }

  startEditDepartment(d: DepartmentDto): void {
    this.editingDepartmentId = d.id;
    this.editDepartmentForm.patchValue({ name: d.name, description: d.description ?? '' });
  }

  cancelEditDepartment(): void {
    this.editingDepartmentId = null;
    this.editDepartmentForm.reset();
  }

  saveEditDepartment(): void {
    if (this.editDepartmentForm.invalid || this.editingDepartmentId === null) { this.editDepartmentForm.markAllAsTouched(); return; }
    const v = this.editDepartmentForm.value;
    this.isSavingEdit = true;
    this.masterDataService.updateDepartment(this.editingDepartmentId, { name: v.name.trim(), description: v.description?.trim() || null })
      .pipe(finalize(() => (this.isSavingEdit = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.editingDepartmentId = null;
          this.loadAllData();
        },
      });
  }

  startEditDesignation(d: DesignationDto): void {
    this.editingDesignationId = d.id;
    this.editDesignationForm.patchValue({ name: d.name, description: d.description ?? '' });
  }

  cancelEditDesignation(): void {
    this.editingDesignationId = null;
    this.editDesignationForm.reset();
  }

  saveEditDesignation(): void {
    if (this.editDesignationForm.invalid || this.editingDesignationId === null) { this.editDesignationForm.markAllAsTouched(); return; }
    const v = this.editDesignationForm.value;
    this.isSavingEdit = true;
    this.masterDataService.updateDesignation(this.editingDesignationId, { name: v.name.trim(), description: v.description?.trim() || null })
      .pipe(finalize(() => (this.isSavingEdit = false)))
      .subscribe({
        next: (res) => {
          this.toastService.showSuccess(res.message);
          this.editingDesignationId = null;
          this.loadAllData();
        },
      });
  }

  openDeleteDialog(type: 'department' | 'designation', id: number, name: string): void {
    this.pendingDeleteAction = { type, id };
    this.dialogTitle = `Delete ${type === 'department' ? 'Department' : 'Designation'}`;
    this.dialogMessage = `Are you sure you want to permanently delete ${name}? This action cannot be undone.`;
    this.dialogBoldText = name;
    this.dialogConfirmText = 'Delete';
    this.dialogVariant = 'danger';
    this.dialogOpen = true;
  }

  onDialogConfirm(): void {
    if (!this.pendingDeleteAction) return;
    const { type, id } = this.pendingDeleteAction;
    this.dialogOpen = false;
    this.isSavingEdit = true;

    const deleteObservable = type === 'department'
      ? this.masterDataService.deleteDepartment(id)
      : this.masterDataService.deleteDesignation(id);

    deleteObservable.subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message);
        if (type === 'department') this.editingDepartmentId = null;
        else this.editingDesignationId = null;
        this.loadAllData();
        this.isSavingEdit = false;
      },
      error: () => {
        this.toastService.showError(`Failed to delete ${type}.`);
        this.isSavingEdit = false;
      }
    });
    this.pendingDeleteAction = null;
  }

  onDialogCancel(): void {
    this.dialogOpen = false;
    this.pendingDeleteAction = null;
  }

  isDepartmentInvalid(field: string): boolean {
    const c = this.departmentForm.get(field);
    return !!(c && c.invalid && c.touched);
  }

  isDesignationInvalid(field: string): boolean {
    const c = this.designationForm.get(field);
    return !!(c && c.invalid && c.touched);
  }
}