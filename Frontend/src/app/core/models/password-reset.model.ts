// ============================================================
// PASSWORD RESET MODELS (Shared)
// Employee, Admin, Manager — sabke liye common hain, kyunki
// backend ka AuthController/forgot-password aur /reset-password
// email se hi user resolve karte hain (koi role-specific split nahi).
// ============================================================

// Step 1 : Forgot password request - Backend AuthController / forgout-password
export interface ForgotPassowrdDto {
    email: string;
}

// Step 2 : OTP + New Password - Backend AuthController / reset-password
export interface ResetPassowrdDto {
    email: string;
    code: string;
    newPassword: string;
    confirmNewPassword: string;
}

export interface ResetPasswordResponse {
  role: 'Admin' | 'Manager' | 'Employee';
}