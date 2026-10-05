// ============================================================
// ADMIN MODELS
// Ye specifically Admin ke flow ke liye hain.
// Naming: Backend ke "AdminRegisterDto" aur "AdminLoginDto" se match karta hai.
// ============================================================

// Admin Registration (Step 1) - Backend AdminController/register
export interface AdminRegisterDto {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  confirmPassword: string;
  phoneNumber: string;
}

// Admin Login (Step 1) - Backend AdminController/login
export interface AdminLoginDto {
  email: string;
  password: string;
  // Backend check karta hai ki User ke Employee record ka phone number isse match karta hai ya nahi.
  phoneNumber: string;
}

// Replace Admin ke liye
export interface ReplaceAdminDto {
  currentPassword: string;
  newAdminFirstName: string;
  newAdminLastName: string;
  newAdminEmail: string;
  newAdminPhoneNumber: string;
  newAdminPassword: string;
  newAdminConfirmPassword: string;
}

// Admin Profile 
export interface AdminProfileDto {
  userId: number;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string;
  presentAddress: string | null;
  dateOfBirth: string | null;
}

// Admin Profile Update
export interface UpdateAdminProfileDto {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  presentAddress: string | null;
  dateOfBirth: string | null;
}

// Admin Password Change
export interface ChangePasswordDto {
  currentPassword: string;
  newPassword: string;
  confirmNewPassword: string;
}