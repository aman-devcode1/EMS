/**
 * Centralized route paths for the entire application.
 * Koi bhi URL yahan se reference karo — kabhi hardcode mat karo.
 *
 * Convention: NO leading slash — Angular routes ke liye standard hai.
 * `router.navigate([ROUTES.ADMIN_LOGIN])` bhi kaam karega.
 */

export const ROUTES = {
  // ============================================================
  // AUTH / PUBLIC
  // ============================================================
  ADMIN_LOGIN: 'admin/login',
  ADMIN_REGISTER: 'admin/register',
  ADMIN_VERIFY_OTP: 'admin/verify-otp',

  EMPLOYEE_LOGIN: 'employee/login',
  EMPLOYEE_REGISTER: 'employee/register',
  EMPLOYEE_VERIFY_OTP: 'employee/verify-otp',

  FORGOT_PASSWORD: 'forgot-password',
  RESET_PASSWORD: 'reset-password',

  // ============================================================
  // ADMIN AREA (Protected)
  // ============================================================
  ADMIN_DASHBOARD: 'admin/dashboard',
  ADMIN_PROFILE: 'admin/profile',
  ADMIN_REPLACE: 'admin/replace-admin',
  ADMIN_EMPLOYEES: 'admin/employees',
  ADMIN_MANAGERS: 'admin/managers',
  ADMIN_MASTER_DATA: 'admin/master-data',

  // ============================================================
  // EMPLOYEE AREA (Protected)
  // ============================================================
  EMPLOYEE_DASHBOARD: 'employee/dashboard',
  EMPLOYEE_PROFILE: 'employee/profile',

  // ============================================================
  // ROOT / FALLBACK
  // ============================================================
  ROOT: '',
} as const;

/**
 * Login URL map — role based routing ke liye.
 * AuthService aur Guards isse use karenge.
 */
export const LOGIN_URL_BY_ROLE: Record<string, string> = {
  Admin: `/${ROUTES.ADMIN_LOGIN}`,
  Manager: `/${ROUTES.ADMIN_LOGIN}`,     // Manager admin portal use karta hai
  Employee: `/${ROUTES.EMPLOYEE_LOGIN}`,
};