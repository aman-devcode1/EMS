// ============================================================
// EMPLOYEE MODELS
// Ye specifically Employee ke self-register + simple (no-OTP) login flow ke liye hain.
// Naming: Backend ke "RegisterDto" aur "LoginDto" (EMS.Core.DTOs.Auth) se match karta hai.
// ============================================================

// Employee Self-Registration - Backend AuthController/register
export interface RegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  phoneNumber: string;
  dateOfBirth: string;           // ISO date string (jaise "2000-05-14")
  presentAddress?: string;       // Optional — backend mein string? hai
  previousCompanyRole?: string;  // Optional
}

// Employee Login - Backend AuthController/login
// Employee ko seedha token milta hai — OTP nahi lagta (Admin/Manager alag,
// AdminLoginDto wale 2-step OTP flow se login karte hain — /api/Admin/login).
export interface LoginDto {
  email: string;
  password: string;
  phoneNumber: string;
}

// Employee Create (Admin bnata hai) - Backend EmployeesController/create
export interface EmployeeCreateDto {
  firstName: string;
  lastName: string;
  email: string;
  department: string;
  designation: string;
  // password: string;
  salary: number;
  hireDate: string;
  dateOfBirth: string;
  phoneNumber?: string;
  presentAddress?: string;
  previousCompanyRole?: string;
  userId?: number;
}

// Employee Response - Backend EmployeesController (GET, CREATE, UPDATE ke response)
export interface EmployeeResponseDto {
  id: number;
  userId: number;
  fullName: string;
  email: string;
  departmentId?: number | null;
  department?: string | null;
  designationId?: number | null;
  designation?: string | null;
  phoneNumber?: string | null;
  presentAddress?: string | null;
  dateOfBirth?: string | null;
  hireDate?: string | null;
  salary: number;
  previousCompanyRole: string;
  role: string;
  isActive: boolean;
  createdAt: string;
}

// Employee Query Params (Pagination + Filter + Sort) - Backend EmployeesController/getAll
export interface EmployeeQueryParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  sortBy?: string;
  sortDirection?: string;
  department?: string;
  designation?: string;
  isActive?: boolean;
} 

// Update Employee Profile
export interface UpdateEmployeeProfileDto {
  firstName: string;
  lastName?: string | null;
  email: string;
  phoneNumber: string;
  presentAddress?: string | null;
  dateOfBirth?: string | null;
}

// Change Password (Employee)
export interface ChangeEmployeePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}

export interface UpdateEmployeeProfessionalDto {
  salary?: number | null;
  departmentId?: number | null;
  designationId?: number | null;
  previousCompanyRole?: string | null;
}