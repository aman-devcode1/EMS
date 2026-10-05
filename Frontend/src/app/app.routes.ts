import { Routes } from '@angular/router';
import { AdminLogin } from './features/admin/admin-login/admin-login';
import { AdminRegister } from './features/admin/admin-register/admin-register';
import { MainLayout } from './layout/main-layout/main-layout';
import { AuthGuard } from './core/guards/auth-guard';
import { RoleGuard } from './core/guards/role-guard';
import { AdminDashboard } from './features/dashboard/admin-dashboard/admin-dashboard';
import { ReplaceAdmin } from './features/admin/admin-replace/admin-replace';
import { EmployeeRegister } from './features/employee/employee-register/employee-register';
import { EmployeeDashboard } from './features/dashboard/employee-dashboard/employee-dashboard';
import { EmployeeLogin } from './features/employee/employee-login/employee-login';
import { ForgotPassword } from './features/auth/forgot-password/forgot-password';
import { ResetPassword } from './features/auth/reset-password/reset-password';
import { Manager } from './features/manager/manager';
import { EmployeeList } from './features/employee-management/employee-list/employee-list';
import { AdminProfile } from './features/admin/admin-profile/admin-profile';
import { VerifyOtp } from './features/auth/verify-otp/verify-otp';
import { EmployeeProfile } from './features/employee/employee-profile/employee-profile';
import { EmployeeDetail } from './features/employee-management/employee-detail/employee-detail';
import { MasterData } from './features/admin/master-data/master-data';

// ✅ Local constants — apni routes.ts file se
import { ROUTES } from './core/constants/routes';
import { Role } from './core/enums/role';

export const routes: Routes = [
  // ============================================================
  // DEFAULT
  // ============================================================
  { path: '', redirectTo: ROUTES.ADMIN_LOGIN, pathMatch: 'full' },

  // ============================================================
  // PUBLIC — ADMIN
  // ============================================================
  { path: ROUTES.ADMIN_LOGIN, component: AdminLogin },
  { path: ROUTES.ADMIN_REGISTER, component: AdminRegister },

  // ============================================================
  // PUBLIC — EMPLOYEE
  // ============================================================
  { path: ROUTES.EMPLOYEE_LOGIN, component: EmployeeLogin },
  { path: ROUTES.EMPLOYEE_REGISTER, component: EmployeeRegister },

  // ============================================================
  // PUBLIC — PASSWORD RESET
  // ============================================================
  { path: ROUTES.FORGOT_PASSWORD, component: ForgotPassword },
  { path: ROUTES.RESET_PASSWORD, component: ResetPassword },

  // ============================================================
  // OTP VERIFY (role-specific)
  // ============================================================
  {
    path: ROUTES.ADMIN_VERIFY_OTP,
    component: VerifyOtp,
    data: { role: Role.Admin }
  },
  {
    path: ROUTES.EMPLOYEE_VERIFY_OTP,
    component: VerifyOtp,
    data: { role: Role.Employee }
  },

  // ============================================================
  // EMPLOYEE AREA (protected)
  // ============================================================
  {
    path: 'employee',
    component: MainLayout,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: Role.Employee },
    children: [
      { path: 'dashboard', component: EmployeeDashboard },
      { path: 'profile', component: EmployeeProfile },
    ],
  },

  // ============================================================
  // ADMIN AREA (protected)
  // ============================================================
  {
    path: 'admin',
    component: MainLayout,
    canActivate: [AuthGuard, RoleGuard],
    data: { roles: Role.Admin },
    children: [
      { path: 'dashboard', component: AdminDashboard },
      { path: 'replace-admin', component: ReplaceAdmin },
      { path: 'employees', component: EmployeeList },
      { path: 'employees/:id', component: EmployeeDetail },
      { path: 'managers', component: Manager },
      { path: 'profile', component: AdminProfile },
      { path: 'master-data', component: MasterData },
    ],
  },
];