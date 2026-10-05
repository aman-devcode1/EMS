import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root',
})
export class TokenService {
  private readonly LAST_ROLE_KEY = 'ems_last_role';

  // ✅ Access token ONLY in memory — page reload pe chala jayega
  // Refresh token ab HttpOnly cookie mein hai (JS access nahi kar sakta)
  private accessToken: string | null = null;

  // ============================================================
  // ACCESS TOKEN
  // ============================================================
  setAccessToken(token: string): void {
    this.accessToken = token;
  }

  getAccessToken(): string | null {
    return this.accessToken;
  }

  clearAccessToken(): void {
    this.accessToken = null;
  }

  // Legacy aliases (purane components ke liye)
  clearTokens(): void {
    this.clearAccessToken();
  }

  clearToken(): void {
    this.clearAccessToken();
  }

  // ============================================================
  // LAST ROLE (routing hint, persists across sessions)
  // ============================================================
  setLastRole(role: string): void {
    localStorage.setItem(this.LAST_ROLE_KEY, role);
  }

  getLastRole(): string | null {
    return localStorage.getItem(this.LAST_ROLE_KEY);
  }

  clearLastRole(): void {
    localStorage.removeItem(this.LAST_ROLE_KEY);
  }

  // ============================================================
  // ROLE FROM CURRENT ACCESS TOKEN
  // ============================================================
  getUserRole(): string | null {
    return this.decodeTokenClaim<string>('role');
  }

  isAdmin(): boolean {
    return this.getUserRole() === 'Admin';
  }

  isManager(): boolean {
    return this.getUserRole() === 'Manager';
  }

  isEmployee(): boolean {
    return this.getUserRole() === 'Employee';
  }

  getUserId(): number | null {
    const uid = this.decodeTokenClaim<string>('uid');
    return uid ? parseInt(uid, 10) : null;
  }

  getUserName(): string | null {
    return this.decodeTokenClaim<string>('name');
  }

  private decodeTokenClaim<T>(claimKey: string): T | null {
    const token = this.accessToken;
    if (!token) return null;

    try {
      const payload = token.split('.')[1];
      const decodedPayload = JSON.parse(atob(payload));
      return decodedPayload[claimKey] ?? null;
    } catch (e) {
      console.error(`Failed to decode "${claimKey}" from token.`, e);
      return null;
    }
  }
}