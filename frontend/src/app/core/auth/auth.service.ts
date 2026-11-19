/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { ApiResponse, AuthResponse, CurrentUser } from '../models/api-response.model';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  // Reactive State Signals
  readonly currentUser = signal<CurrentUser | null>(this.getInitialUser());
  readonly isAuthenticated = computed(() => this.currentUser() !== null);
  readonly roles = computed(() => this.currentUser()?.roles ?? []);

  login(credentials: { email: string; password: string }): Observable<ApiResponse<AuthResponse>> {
    return this.http.post<ApiResponse<AuthResponse>>('/auth/login', credentials).pipe(
      tap((res) => {
        if (res.success && res.data) {
          const authData = res.data;
          localStorage.setItem('access_token', authData.token);
          localStorage.setItem('refresh_token', authData.refreshToken);

          const user: CurrentUser = {
            id: authData.userId,
            email: authData.email,
            fullName: authData.fullName,
            roles: authData.roles,
          };
          localStorage.setItem('user_info', JSON.stringify(user));
          this.currentUser.set(user);
        }
      }),
    );
  }

  logout(): void {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user_info');
    this.currentUser.set(null);
    this.router.navigate(['/auth/login']);
  }

  hasRole(role: string): boolean {
    return this.roles().includes(role) || this.roles().includes('SuperAdmin');
  }

  hasAnyRole(requiredRoles: string[]): boolean {
    if (this.roles().includes('SuperAdmin')) return true;
    return requiredRoles.some((r) => this.roles().includes(r));
  }

  private getInitialUser(): CurrentUser | null {
    try {
      const stored = localStorage.getItem('user_info');
      return stored ? JSON.parse(stored) : null;
    } catch {
      return null;
    }
  }
}
