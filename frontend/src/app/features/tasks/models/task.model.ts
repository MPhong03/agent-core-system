/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file task.model.ts — Định nghĩa kiểu dữ liệu & interfaces cho phân hệ Quản lý Tác vụ Kanban & Review Gate (Sprint 2 - PBI-TASK-01)
 *
 * ## Cấu trúc Dữ liệu Phân Hệ Tasks
 * Khớp hợp đồng REST API v1 (`/api/v1/channels/{channelId}/tasks`) và SignalR Tasks Hub (`/hubs/tasks`).
 *
 * ## Ràng buộc Kỹ thuật & Khớp Hợp đồng FE ↔ BE (RULE-01)
 * 🔴 UUIDv7: Toàn bộ khóa chính `id`, `channelId`, `taskId`, `memberId` là chuỗi UUIDv7.
 * 🔴 Cổng Nghiệm Thu: Trạng thái `InReview` là Review Gate, nơi chỉ con người mới có quyền `Approve` hoặc `RequestRevision`.
 * ⛔ Không đổi tên các event SignalR: `TaskCreated`, `TaskStatusChanged`, `TaskAssigned`, `TaskArtifactAdded`, `TaskReviewDecision`.
 */

export type TaskItemStatus = 'ToDo' | 'InProgress' | 'InReview' | 'Done' | 'Failed';

export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Urgent';

export interface TaskAssignment {
  id: string;
  taskId: string;
  assigneeMemberId: string;
  isAgent: boolean;
  roleInTask: string;
}

export interface TaskArtifact {
  id: string;
  taskId: string;
  fileName: string;
  filePath: string;
  fileType: string;
  fileSize: number;
  summary: string;
  createdByMemberId: string;
  createdDate: string;
}

export interface TaskActivityLog {
  id: string;
  taskId: string;
  actorMemberId: string;
  actionType: string;
  fromStatus: TaskItemStatus | null;
  toStatus: TaskItemStatus | null;
  comment: string | null;
  createdDate: string;
}

export interface TaskItem {
  id: string;
  channelId: string;
  taskCode: string;
  title: string;
  description: string;
  status: TaskItemStatus;
  priority: TaskPriority;
  dueDate: string | null;
  startedDate: string | null;
  completedDate: string | null;
  createdDate: string;
  assignments: TaskAssignment[];
}

export interface TaskDetail extends TaskItem {
  artifacts: TaskArtifact[];
  activityLogs: TaskActivityLog[];
}

export interface CreateTaskRequest {
  title: string;
  description: string;
  priority: TaskPriority;
  dueDate?: string | null;
  assigneeMemberId?: string | null;
  isAgent?: boolean | null;
}

export interface MoveTaskStatusRequest {
  newStatus: TaskItemStatus;
  comment?: string;
}

export interface AssignTaskRequest {
  assigneeMemberId: string;
  isAgent: boolean;
  roleInTask: string;
}

export interface ReviewTaskRequest {
  isApproved: boolean;
  feedback?: string;
}

export interface TaskStatusChangedEvent {
  taskId: string;
  oldStatus: string;
  newStatus: string;
  actorMemberId: string;
}

export interface TaskReviewDecisionEvent {
  taskId: string;
  decision: string;
  feedback: string | null;
  reviewerMemberId: string;
}
