import { CommonModule } from '@angular/common';
import { Component, ContentChild, EventEmitter, Input, Output, TemplateRef } from '@angular/core';

export interface DataTableColumn {
  key: string;
  label: string;
  sortable?: boolean;
}

@Component({
  selector: 'app-data-table',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './data-table.html',
  styleUrl: './data-table.css',
})
export class DataTable {
  @Input() columns: DataTableColumn[] = [];
  @Input() data: any[] = [];
  @Input() loading = false;
  @Input() emptyMessage = 'No Records Found.';
  @Input() sortKey: string | null = null;
  @Input() sortDirection: 'asc' | 'desc' = 'asc';

  @Output() sortChange = new EventEmitter<{ key: string; direction: 'asc' | 'desc' }>();

  // Parent apne row-level Edit/Delete buttons is templete se inject karega
  @ContentChild('rowActions') rowActionsTemplate?: TemplateRef<any>;

  onSort(column: DataTableColumn) {
    if (!column.sortable) return;
    const direction: 'asc' | 'desc' = this.sortKey === column.key && this.sortDirection === 'asc' ? 'desc' : 'asc';
    this.sortKey = column.key;
    this.sortDirection = direction;
    this.sortChange.emit({ key: column.key, direction });
  }
}