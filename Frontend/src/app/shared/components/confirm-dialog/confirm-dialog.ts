import { Component, EventEmitter, HostListener, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-dialog',
  imports: [CommonModule],
  standalone: true,
  templateUrl: './confirm-dialog.html',
  styleUrl: './confirm-dialog.css',
})
export class ConfirmDialog {
  @Input() isOpen = false;
  @Input() title = 'Confirm action';
  @Input() message = '';
  @Input() boldText = '';          // 🔥 name/text to bold inside message
  @Input() confirmText = 'Confirm';
  @Input() cancelText = 'Cancel';
  @Input() variant: 'primary' | 'danger' = 'primary';

  @Output() confirmed = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  // Splits message around boldText so we can render <strong>
  get messageParts(): { before: string; bold: string; after: string } {
    if (!this.boldText) return { before: this.message, bold: '', after: '' };
    const idx = this.message.indexOf(this.boldText);
    if (idx === -1) return { before: this.message, bold: '', after: '' };
    return {
      before: this.message.substring(0, idx),
      bold: this.boldText,
      after: this.message.substring(idx + this.boldText.length),
    };
  }

  onConfirm(): void { this.confirmed.emit(); }
  onCancel(): void { this.cancelled.emit(); }
  onOverlayClick(): void { this.onCancel(); }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    if (this.isOpen) this.onCancel();
  }
}