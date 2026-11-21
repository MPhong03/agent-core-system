/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tools.service.ts — Quản lý State và REST API cho Công cụ AI (Sprint 3 - PBI-TOOL-01)
 *
 * ## Vai trò Kiến trúc
 * - Quản lý Reactive State bằng Angular Signals (`tools`, `isLoading`, `selectedTool`).
 * - Giao tiếp với backend endpoints `/api/v1/agents/tools`.
 * - Hỗ trợ các thao tác CRUD và kiểm thử Tool (Ad-hoc và Saved Tool).
 */

import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap, catchError, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { ApiResponse } from '../../../../core/models/api-response.model';
import {
  ToolDefinitionDto,
  CreateToolRequest,
  UpdateToolRequest,
  TestToolRequest,
  TestAdHocToolRequest,
  ToolTestResultDto,
} from '../models/tool.models';

@Injectable({
  providedIn: 'root',
})
export class ToolsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/agents/tools`;

  // ── Reactive Signals State ──
  readonly tools = signal<ToolDefinitionDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly selectedTool = signal<ToolDefinitionDto | null>(null);

  /**
   * Tải danh sách tất cả định nghĩa công cụ từ máy chủ.
   * Cập nhật danh sách vào `tools` signal.
   */
  loadTools(): Observable<ApiResponse<ToolDefinitionDto[]>> {
    this.isLoading.set(true);

    return this.http.get<ApiResponse<ToolDefinitionDto[]>>(this.baseUrl).pipe(
      tap({
        next: (res) => {
          this.isLoading.set(false);
          if (res.success && res.data) {
            this.tools.set(res.data);
          }
        },
        error: (err) => {
          this.isLoading.set(false);
          return throwError(() => err);
        },
      }),
      catchError((err) => {
        this.isLoading.set(false);
        return throwError(() => err);
      }),
    );
  }

  /**
   * Truy vấn thông tin chi tiết một công cụ theo ID (UUIDv7).
   */
  getToolById(id: string): Observable<ApiResponse<ToolDefinitionDto>> {
    return this.http.get<ApiResponse<ToolDefinitionDto>>(`${this.baseUrl}/${id}`);
  }

  /**
   * Tạo mới một định nghĩa công cụ và đưa vào state.
   */
  createTool(request: CreateToolRequest): Observable<ApiResponse<ToolDefinitionDto>> {
    return this.http.post<ApiResponse<ToolDefinitionDto>>(this.baseUrl, request).pipe(
      tap((res) => {
        if (res.success && res.data) {
          this.tools.update((current) => [res.data, ...current]);
        }
      }),
    );
  }

  /**
   * Cập nhật thông tin cấu hình công cụ hiện có.
   */
  updateTool(id: string, request: UpdateToolRequest): Observable<ApiResponse<ToolDefinitionDto>> {
    return this.http.put<ApiResponse<ToolDefinitionDto>>(`${this.baseUrl}/${id}`, request).pipe(
      tap((res) => {
        if (res.success && res.data) {
          this.tools.update((current) =>
            current.map((tool) => (tool.id === id ? res.data : tool)),
          );
        }
      }),
    );
  }

  /**
   * Xóa một định nghĩa công cụ và gỡ khỏi danh sách state.
   */
  deleteTool(id: string): Observable<ApiResponse<string>> {
    return this.http.delete<ApiResponse<string>>(`${this.baseUrl}/${id}`).pipe(
      tap((res) => {
        if (res.success) {
          this.tools.update((current) => current.filter((tool) => tool.id !== id));
        }
      }),
    );
  }

  /**
   * Chạy thử nghiệm công cụ đã lưu trong hệ thống với các tham số mẫu.
   */
  testSavedTool(id: string, request: TestToolRequest): Observable<ApiResponse<ToolTestResultDto>> {
    return this.http.post<ApiResponse<ToolTestResultDto>>(`${this.baseUrl}/${id}/test`, request);
  }

  /**
   * Chạy thử nghiệm công cụ ad-hoc (chưa lưu vào DB) với URL và tham số tự do.
   */
  testAdHocTool(request: TestAdHocToolRequest): Observable<ApiResponse<ToolTestResultDto>> {
    return this.http.post<ApiResponse<ToolTestResultDto>>(`${this.baseUrl}/test`, request);
  }

  /**
   * Thiết lập hoặc giải phóng công cụ đang được chọn để xem/chỉnh sửa.
   */
  setSelectedTool(tool: ToolDefinitionDto | null): void {
    this.selectedTool.set(tool);
  }

  /**
   * Lấy danh sách các công cụ được cấp quyền cho một Agent Profile.
   */
  getAgentTools(agentId: string): Observable<ApiResponse<ToolDefinitionDto[]>> {
    return this.http.get<ApiResponse<ToolDefinitionDto[]>>(
      `${environment.apiUrl}/agents/profiles/${agentId}/tools`,
    );
  }

  /**
   * Cập nhật danh sách công cụ được phân quyền cho một Agent Profile.
   */
  assignAgentTools(
    agentId: string,
    toolDefinitionIds: string[],
  ): Observable<ApiResponse<string>> {
    return this.http.put<ApiResponse<string>>(
      `${environment.apiUrl}/agents/profiles/${agentId}/tools`,
      { toolDefinitionIds },
    );
  }
}
