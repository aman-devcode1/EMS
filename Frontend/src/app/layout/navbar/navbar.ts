import { Component, EventEmitter, HostListener, Output } from '@angular/core';
import { ToastService } from '../../core/services/toast';
import { Router, RouterLink } from '@angular/router';
import { TokenService } from '../../core/services/token';
import { CommonModule } from '@angular/common';
import { AuthService } from '../../core/services/auth';

@Component({
  selector: 'app-navbar',
  imports: [CommonModule, RouterLink],
  standalone: true,
  templateUrl: './navbar.html',
  styleUrl: './navbar.css',
})
export class Navbar {
  @Output() menuToggle = new EventEmitter<void>();
  profileMenuOpen = false;

  constructor(
    private toastService: ToastService,
    private authService: AuthService,
    private router: Router,
    private tokenService: TokenService,
  ) { }

  onMenuClick(): void {
    this.menuToggle.emit();
  }

  toggleProfileMenu(event: MouseEvent): void {
    event.stopPropagation();
    this.profileMenuOpen = !this.profileMenuOpen;
  }

  closeProfileMenu(): void {
    this.profileMenuOpen = false;
  }

  // 🔥 Role-based profile route
  get profileRoute(): string {
    return this.tokenService.isAdmin() ? '/admin/profile' : '/employee/profile';
  }

  // 🔥 Role-based sign out
  onSignOut(): void {
    this.authService.logout().subscribe({
      next: () => {
        this.router.navigate([this.authService.getLoginUrl()]);
      },
      error: () => {
        this.router.navigate([this.authService.getLoginUrl()]);
      }
    });
  }

  getInitials(): string {
    const name = this.tokenService.getUserName();
    if (!name) return '?';
    const parts = name.trim().split(/\s+/);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    if (this.profileMenuOpen) this.profileMenuOpen = false;
  }
}