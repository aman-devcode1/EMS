import { CommonModule } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { Params, RouterLink } from '@angular/router';
import { AdminService } from '../../../core/services/admin';
import { LandingStatus } from '../../../core/models/landing.model';

type PortalKey = 'admin' | 'manager' | 'employee';
type PortalIcon = 'shield' | 'briefcase' | 'user';

interface Portal {
  key: PortalKey;
  title: string;
  summary: string;
  points: string[];
  icon: PortalIcon;
  loginLink: string;
  loginQueryParams?: Params;
  registerLink?: string;
  registerLabel?: string;
}

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
})
export class LandingPage implements OnInit {
  private readonly adminService = inject(AdminService);

  readonly year = new Date().getFullYear();

  // null = abhi load ho raha hai
  readonly status = signal<LandingStatus | null>(null);
  readonly statusFailed = signal(false);

  readonly portals: Portal[] = [
    {
      key: 'admin',
      title: 'Admin',
      summary: 'Run the system: people, managers, master data and admin succession.',
      points: [
        'Manage employees and departments',
        'Assign managers and master data',
        'Secure sign-in with a one-time code',
      ],
      icon: 'shield',
      loginLink: '/admin/login',
      registerLink: '/admin/register',
      registerLabel: 'Register',
    },
    {
      key: 'manager',
      title: 'Manager',
      summary: 'View your team and keep your own profile up to date.',
      points: ['View employee details', 'Update your profile and password'],
      icon: 'briefcase',
      loginLink: '/admin/login',
      loginQueryParams: { role: 'manager' }
    },
    {
      key: 'employee',
      title: 'Employee',
      summary: 'View your profile, payroll and team details in one place.',
      points: ['Personal and professional details', 'New here? Verify your email with a code'],
      icon: 'user',
      loginLink: '/employee/login',
      registerLink: '/employee/register',
      registerLabel: 'Register',
    },
  ];

  ngOnInit(): void {
    this.adminService.getAdminStatus().subscribe({
      next: (res) => this.status.set(res.data),
      error: () => this.statusFailed.set(true),
    });
  }

  // Sign in tabhi jab us role ka account exist karta ho
  canSignIn(portal: Portal): boolean {
    const s = this.status();
    if (!s) return false;

    switch (portal.key) {
      case 'admin': return s.adminExists;
      case 'manager': return s.managerExists;
      case 'employee': return s.employeeExists;
    }
  }

  // Admin sirf tab register ho jab koi admin na ho. Employee hamesha.
  canRegister(portal: Portal): boolean {
    if (!portal.registerLink) return false;

    if (portal.key === 'admin') {
      const s = this.status();
      return !!s && !s.adminExists;
    }
    return true;
  }

  statusNote(portal: Portal): string | undefined {
    if (this.statusFailed()) return 'Could not check account status. Please refresh the page.';

    const s = this.status();
    if (!s) return 'Checking account status…';

    switch (portal.key) {
      case 'admin':
        return s.adminExists
          ? 'Administrator is already set up. Change it from Replace Admin inside the admin panel.'
          : 'No administrator yet. Register to set up the first admin account.';
      case 'manager':
        return s.managerExists
          ? 'Manager access is granted by an administrator.'
          : 'No manager yet. An administrator promotes an employee to manager.';
      case 'employee':
        return s.employeeExists ? undefined : 'No employee accounts yet. Register to get started.';
    }
  }
}