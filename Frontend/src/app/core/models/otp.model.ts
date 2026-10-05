// ============================================================
// OTP MODELS (Shared)
// Ye Models Admin aur Employee DONO ke liye common hain.
// Kyu? Kyunki backend ke AuthController aur AdminController dono ne `ResendOtpDto` aur `VerifyOtpDto` ko same structure mein banaya hai.
// ============================================================

// Ye tab response mein aata hai jab OTP successfully send ho jata hai.
export interface OtpSentResponseDto {
  email: string; // Email jispar OTP bheja gaya (UI mein dikhane ke liye)
}

// Ye 6-digit OTP ko verify karne ke liye hai (Login OTP ya Registration OTP dono ke liye same).
export interface VerifyOtpDto {
  email: string; // Email jispar OTP bheja jayega
  code: string; // User ne jo 6-digit OTP code enter karega (Backend se email mein aata hai)
}

// Ye otp resend karne ke liye hai (Login OTP ya Registration OTP dono ke liye same).
export interface ResendOtpDto {
  // Sirf email bhejte hain.
  // Backend khud User ko email se dhundega aur decide karega ki
  // woh Registration ka user hai (Inactive) ya Login ka user hai (Active).
  // Isliye ye DONO ke liye shared hai.
  email: string; // Email jispar OTP bheja jayega (UI mein dikhane ke liye)
  purpose: number;
}