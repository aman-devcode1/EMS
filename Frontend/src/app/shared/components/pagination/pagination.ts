import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-pagination',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './pagination.html',
  styleUrl: './pagination.css',
})
export class Pagination {
  @Input() pageNumber = 1;
  @Input() totalPages = 1;
  @Input() hasPreviousPage = false;
  @Input() hasNextPage = false;

  @Output() pageChange = new EventEmitter<number>();

  // Current page ke aas-paas ke pages + hamesha 1 aur last page dikhata hai
  get pages(): number[] {
    const total = this.totalPages;
    const current = this.pageNumber;
    const windowSize = 1;

    const result: number[] = [];
    for (let p = 1; p <= total; p++) {
      const isEdge = p === 1 || p === total;
      const isNearCurrent = Math.abs(p - current) <= windowSize;
      if (isEdge || isNearCurrent) result.push(p);
    }
    return result;
  }

  // Beech mein gap ho to "…" dikhane ke liye
  get pagesWithGaps(): (number | 'gap')[] {
    const result: (number | 'gap')[] = [];
    let prev = 0;
    for (const p of this.pages) {
      if (prev && p - prev > 1) result.push('gap');
      result.push(p);
      prev = p;
    }
    return result;
  }

  goTo(page: number) {
    if (page < 1 || page > this.totalPages || page === this.pageNumber) return;
    this.pageChange.emit(page);
  }

  previous() {
    if (this.hasPreviousPage) this.goTo(this.pageNumber - 1);
  }

  next() {
    if (this.hasNextPage) this.goTo(this.pageNumber + 1);
  }
}