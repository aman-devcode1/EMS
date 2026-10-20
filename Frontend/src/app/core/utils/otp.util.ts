// OTP input ko sirf digits rakhta hai aur max length tak cut karta hai.
export function sanitizeOtp(value: string | null | undefined, length = 6): string {
  return (value ?? '').replace(/\D/g, '').substring(0, length);
}