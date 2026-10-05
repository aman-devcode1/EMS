import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TokenService } from '../../core/services/token';
import { ToastService } from '../../core/services/toast';

@Component({
  selector: 'app-sidebar',
  imports: [RouterLink, CommonModule, RouterLinkActive],
  standalone: true,
  templateUrl: './sidebar.html',
  styleUrl: './sidebar.css',
})
export class Sidebar {
  @Input() isOpen = false;
  @Output() requestClose = new EventEmitter<void>();

  constructor(
    public tokenService: TokenService,
    private toastService: ToastService,
  ) {}

  close(): void {
    this.requestClose.emit();
  }

  // 🔥 Coming soon features के लिए
  onComingSoon(featureName: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.toastService.showSuccess(`${featureName} — coming soon. Stay tuned!`);
    // Mobile पर sidebar close
    this.close();
  }
}