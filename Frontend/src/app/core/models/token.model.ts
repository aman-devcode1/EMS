// Admin aur Employee dono ko login/register ke baad ye milta hai
export interface TokenResponseDto {
  accessToken: string; // Short-lived token (15-60 minutes)
  refreshToken: string; // Short-lived token (1-7 days)
  expiresIn: number; // 900 seconds = 15 minutes
  tokenType: string; // "Bearer"
}