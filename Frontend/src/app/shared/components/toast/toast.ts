import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Subscription } from 'rxjs';
import { Toast, ToastService } from '../../../core/services/toast';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast.html',
  styleUrls: ['./toast.css']
})
export class ToastComponent implements OnInit, OnDestroy {
  toasts: Toast[] = [];
  private subscription: Subscription | undefined;
  private timeouts = new Map<number, ReturnType<typeof setTimeout>>();

  constructor(private toastService: ToastService) {}

  ngOnInit() {
    this.subscription = this.toastService.toasts$.subscribe((toast: Toast) => {
      this.toasts.push(toast);
      const timeoutId = setTimeout(() => this.remove(toast.id), 4000);
      this.timeouts.set(toast.id, timeoutId);
    });
  }

  ngOnDestroy() {
    this.subscription?.unsubscribe();
    this.timeouts.forEach(id => clearTimeout(id));
    this.timeouts.clear();
  }

  // Toast ko click karne par remove karna
  remove(id: number) {
    this.toasts = this.toasts.filter(t => t.id !== id);
    const timeoutId = this.timeouts.get(id);
    if (timeoutId) {
      clearTimeout(timeoutId);
      this.timeouts.delete(id);
    }
  }
}