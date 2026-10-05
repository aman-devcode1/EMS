import { Injectable } from '@angular/core';
import { Subject } from 'rxjs/internal/Subject';

export interface Toast {
  id: number;
  type: 'success' | 'error' | 'info' | 'warning';
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class ToastService {
  private toastSubject = new Subject<Toast>();
  toasts$ = this.toastSubject.asObservable();

  // Success Message
  showSuccess(message: string) {
    this.show('success', message);
  }

  // Error Message
  showError(message: string) {
    this.show('error', message);
  }

  // Info Message
  showInfo(message: string) {
    this.show('info', message);
  }

  // Warning Message
  showWarning(message: string) {
    this.show('warning', message);
  }

  private show(type: Toast['type'], message: string) {
    const id = Date.now() + Math.random(); // Unique id for each toast
    this.toastSubject.next({ id, type, message });
  }
}
