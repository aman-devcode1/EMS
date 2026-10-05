// ============================================================
// API ENDPOINTS
// Saare Admin ke API paths ek jagah rakhe hain.
// Kyu? Taaki baad mein agar backend ka URL change ho, toh sirf yahan edit karna pade.
// ============================================================

export const API_ENDPOINTS = {
  AUTH: {
    REFRESH: '/api/auth/refresh-token',
    LOGOUT: '/api/auth/logout',
    FORGOT_PASSWORD: '/api/auth/forgot-password',
    RESET_PASSWORD: '/api/auth/reset-password'
  },

  ADMIN: {
    REGISTER: '/api/admin/register', // Post -> AdminRegisterDto
    LOGIN: '/api/admin/login', // Post -> AdminLoginDto
    VERIFY_LOGIN_OTP: '/api/admin/verify-login-otp', // Post -> VerifyOtpDto
    VERIFY_REGISTRATION_OTP: '/api/admin/verify-registration-otp', // Post -> VerifyOtpDto
    RESEND_OTP: '/api/admin/resend-otp', // Post -> ResendOtpDto
    REPLACE_ADMIN: '/api/admin/replace-admin', // Post -> ReplaceAdminDto
    MANAGER: '/api/admin/manager',
    MANAGER_ROLE: '/api/admin/manager',
    ME: '/api/admin/me',
    CHANGE_PASSWORD: '/api/admin/change-password',
    DEPARTMENTS: '/api/admin/departments',
    DESIGNATIONS: '/api/admin/designations',
  },

  EMPLOYEE: {
    REGISTER: '/api/auth/register', // Post -> RegisterDto (Employee)
    LOGIN: '/api/auth/login', // Post -> LoginDto (Employee, no-OTP)
    VERIFY_REGISTRATION_OTP: '/api/auth/verify-registration-otp', // Post -> VerifyOtpDto
    RESEND_OTP: '/api/auth/resend-otp', // Post -> ResendOtpDto
    GET_MY_PROFILE: '/api/Employees/me', // Post -> EmployeeResponseDto
    GET_ALL: '/api/Employees',
  },
};
