/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tasks-http.service.ts — Dịch vụ HTTP gọi REST API Quản lý Tác vụ Kanban (Sprint 2 - PBI-TASK-01)
 *
 * ## Vai trò Kiến trúc
 * Cung cấp các thao tác CRUD và State Machine cho Tác vụ Kanban, phân công và Cổng Nghiệm Thu (Review Gate).
 */

import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { ApiResponse } from '../../../core/models/api-response.model';
import {
  AssignTaskRequest,
  CreateTaskRequest,
  MoveTaskStatusRequest,
  ReviewTaskRequest,
  TaskDetail,
  TaskItem,
} from '../models/task.model';

@Injectable({
  providedIn: 'root',
})
export class TasksHttpService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  /**
   * Lấy danh sách các tác vụ trong kênh để render bảng Kanban.
   */
  getTasks(channelId: string): Observable<ApiResponse<TaskItem[]>> {
    return this.http.get<ApiResponse<TaskItem[]>>(`${this.apiUrl}/channels/${channelId}/tasks`);
  }

  /**
   * Lấy chi tiết đầy đủ của một tác vụ bao gồm artifacts và lịch sử hoạt động.
   */
  getTaskById(taskId: string): Observable<ApiResponse<TaskDetail>> {
    return this.http.get<ApiResponse<TaskDetail>>(`${this.apiUrl}/tasks/${taskId}`);
  }

  /**
   * Tạo một tác vụ mới trên bảng Kanban của kênh.
   */
  createTask(channelId: string, request: CreateTaskRequest): Observable<ApiResponse<TaskItem>> {
    return this.http.post<ApiResponse<TaskItem>>(`${this.apiUrl}/channels/${channelId}/tasks`, request);
  }

  /**
   * Cập nhật trạng thái tác vụ khi kéo thả giữa các cột Kanban.
   */
  moveTaskStatus(taskId: string, request: MoveTaskStatusRequest): Observable<ApiResponse<TaskItem>> {
    return this.http.patch<ApiResponse<TaskItem>>(`${this.apiUrl}/tasks/${taskId}/status`, request);
  }

  /**
   * Phân công thành viên (Human hoặc Agent) chịu trách nhiệm cho tác vụ.
   */
  assignTask(taskId: string, request: AssignTaskRequest): Observable<ApiResponse<TaskItem>> {
    return this.http.post<ApiResponse<TaskItem>>(`${this.apiUrl}/tasks/${taskId}/assign`, request);
  }

  /**
   * Nghiệm thu tác vụ tại Cổng Nghiệm Thu (Approve hoặc Request Revision).
   */
  reviewTask(taskId: string, request: ReviewTaskRequest): Observable<ApiResponse<TaskItem>> {
    return this.http.post<ApiResponse<TaskItem>>(`${this.apiUrl}/tasks/${taskId}/review`, request);
  }

  /**
   * Tải về file artifact đính kèm của tác vụ.
   */
  downloadArtifact(taskId: string, artifactId: string): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/tasks/${taskId}/artifacts/${artifactId}/download`, {
      responseType: 'blob',
    });
  }
}
