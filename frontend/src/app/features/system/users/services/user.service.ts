/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { ApiResponse, PagedResult } from '../../../../core/models/api-response.model';
import {
  UserDto,
  UserDetailDto,
  CreateUserRequest,
  UpdateUserRequest,
  ChangePasswordRequest,
} from '../models/user.model';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  private readonly http = inject(HttpClient);

  // State Signals
  readonly users = signal<UserDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly totalCount = signal<number>(0);
  readonly pageIndex = signal<number>(1);
  readonly pageSize = signal<number>(10);
  readonly filterKeyword = signal<string>('');

  fetchUsers(pageIndex = 1, pageSize = 10, keyword = ''): void {
    this.isLoading.set(true);
    this.pageIndex.set(pageIndex);
    this.pageSize.set(pageSize);
    this.filterKeyword.set(keyword);

    let params = new HttpParams()
      .set('pageIndex', pageIndex.toString())
      .set('pageSize', pageSize.toString());

    if (keyword) {
      params = params.set('keyword', keyword);
    }

    this.http.get<ApiResponse<PagedResult<UserDto>>>('/users', { params }).subscribe({
      next: (res) => {
        this.isLoading.set(false);
        if (res.success && res.data) {
          this.users.set(res.data.items);
          this.totalCount.set(res.data.totalCount);
        }
      },
      error: () => {
        this.isLoading.set(false);
      },
    });
  }

  getUserById(id: string): Observable<ApiResponse<UserDetailDto>> {
    return this.http.get<ApiResponse<UserDetailDto>>(`/users/${id}`);
  }

  createUser(request: CreateUserRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>('/users', request).pipe(tap(() => this.reload()));
  }

  updateUser(id: string, request: UpdateUserRequest): Observable<ApiResponse<string>> {
    return this.http
      .put<ApiResponse<string>>(`/users/${id}`, request)
      .pipe(tap(() => this.reload()));
  }

  deleteUser(id: string): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`/users/${id}`).pipe(tap(() => this.reload()));
  }

  changePassword(userId: string, request: ChangePasswordRequest): Observable<ApiResponse<string>> {
    return this.http.post<ApiResponse<string>>(`/users/${userId}/change-password`, request);
  }

  private reload(): void {
    this.fetchUsers(this.pageIndex(), this.pageSize(), this.filterKeyword());
  }
}
