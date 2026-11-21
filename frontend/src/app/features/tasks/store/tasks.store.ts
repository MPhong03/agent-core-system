/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tasks.store.ts — Quản lý trạng thái bảng Kanban bằng Angular Signals (Sprint 2 - PBI-TASK-01)
 *
 * ## Vai trò Kiến trúc
 * State Store tập trung quản lý danh sách tác vụ theo từng cột trạng thái (`todoTasks`, `inProgressTasks`,
 * `inReviewTasks`, `doneTasks`), đồng bộ hai chiều tức thì với SignalR Tasks Hub.
 */

import { Injectable, computed, inject, signal, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';
import {
  AssignTaskRequest,
  CreateTaskRequest,
  MoveTaskStatusRequest,
  ReviewTaskRequest,
  TaskDetail,
  TaskItem,
  TaskItemStatus,
} from '../models/task.model';
import { TasksHttpService } from '../services/tasks-http.service';
import { TasksSignalRService } from '../services/tasks-signalr.service';

@Injectable({
  providedIn: 'root',
})
export class TasksStore implements OnDestroy {
  private readonly http = inject(TasksHttpService);
  private readonly signalr = inject(TasksSignalRService);
  private readonly subscriptions = new Subscription();

  // ── State Signals ──
  readonly tasks = signal<TaskItem[]>([]);
  readonly selectedTask = signal<TaskDetail | null>(null);
  readonly isLoading = signal<boolean>(false);
  readonly activeChannelId = signal<string | null>(null);

  // ── Computed Columns for Kanban ──
  readonly todoTasks = computed(() => this.tasks().filter((t) => t.status === 'ToDo'));
  readonly inProgressTasks = computed(() => this.tasks().filter((t) => t.status === 'InProgress'));
  readonly inReviewTasks = computed(() => this.tasks().filter((t) => t.status === 'InReview'));
  readonly doneTasks = computed(() => this.tasks().filter((t) => t.status === 'Done'));

  // ── Status Counts ──
  readonly totalTasksCount = computed(() => this.tasks().length);
  readonly inReviewCount = computed(() => this.inReviewTasks().length);

  constructor() {
    this.registerSignalRListeners();
  }

  /**
   * Khởi động và tải dữ liệu tác vụ của kênh làm việc.
   */
  async initChannel(channelId: string): Promise<void> {
    this.activeChannelId.set(channelId);
    await this.loadTasks(channelId);
    await this.signalr.joinChannelTasks(channelId);
  }

  /**
   * Tải danh sách tác vụ từ REST API.
   */
  async loadTasks(channelId: string): Promise<void> {
    this.isLoading.set(true);
    try {
      this.http.getTasks(channelId).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.tasks.set(res.data);
          }
          this.isLoading.set(false);
        },
        error: () => {
          this.isLoading.set(false);
        },
      });
    } catch {
      this.isLoading.set(false);
    }
  }

  /**
   * Tải chi tiết tác vụ bao gồm artifacts và lịch sử.
   */
  loadTaskDetail(taskId: string): void {
    this.http.getTaskById(taskId).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.selectedTask.set(res.data);
        }
      },
    });
  }

  /**
   * Tạo tác vụ mới trên bảng Kanban.
   */
  createTask(channelId: string, request: CreateTaskRequest): Promise<TaskItem> {
    return new Promise((resolve, reject) => {
      this.http.createTask(channelId, request).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.upsertTask(res.data);
            resolve(res.data);
          } else {
            reject(new Error(res.message));
          }
        },
        error: (err) => reject(err),
      });
    });
  }

  /**
   * Cập nhật trạng thái khi kéo thả giữa các cột Kanban.
   */
  moveTaskStatus(taskId: string, newStatus: TaskItemStatus, comment?: string): Promise<TaskItem> {
    // Cập nhật lạc quan (Optimistic Update)
    this.tasks.update((list) =>
      list.map((t) => (t.id === taskId ? { ...t, status: newStatus } : t)),
    );

    return new Promise((resolve, reject) => {
      const request: MoveTaskStatusRequest = { newStatus, comment };
      this.http.moveTaskStatus(taskId, request).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.upsertTask(res.data);
            resolve(res.data);
          } else {
            reject(new Error(res.message));
          }
        },
        error: (err) => {
          // Revert nếu lỗi
          const channelId = this.activeChannelId();
          if (channelId) {
            void this.loadTasks(channelId);
          }
          reject(err);
        },
      });
    });
  }

  /**
   * Phân công người chịu trách nhiệm cho tác vụ.
   */
  assignTask(taskId: string, request: AssignTaskRequest): Promise<TaskItem> {
    return new Promise((resolve, reject) => {
      this.http.assignTask(taskId, request).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.upsertTask(res.data);
            resolve(res.data);
          } else {
            reject(new Error(res.message));
          }
        },
        error: (err) => reject(err),
      });
    });
  }

  /**
   * Nghiệm thu tác vụ tại Review Gate (Approve / Request Revision).
   */
  reviewTask(taskId: string, request: ReviewTaskRequest): Promise<TaskItem> {
    return new Promise((resolve, reject) => {
      this.http.reviewTask(taskId, request).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.upsertTask(res.data);
            resolve(res.data);
          } else {
            reject(new Error(res.message));
          }
        },
        error: (err) => reject(err),
      });
    });
  }

  private upsertTask(task: TaskItem): void {
    this.tasks.update((list) => {
      const index = list.findIndex((t) => t.id === task.id);
      if (index >= 0) {
        const copy = [...list];
        copy[index] = task;
        return copy;
      }
      return [...list, task];
    });
  }

  private registerSignalRListeners(): void {
    this.subscriptions.add(
      this.signalr.taskCreated$.subscribe((task) => {
        if (task.channelId === this.activeChannelId()) {
          this.upsertTask(task);
        }
      }),
    );

    this.subscriptions.add(
      this.signalr.taskStatusChanged$.subscribe((evt) => {
        this.tasks.update((list) =>
          list.map((t) =>
            t.id === evt.taskId ? { ...t, status: evt.newStatus as TaskItemStatus } : t,
          ),
        );
      }),
    );

    this.subscriptions.add(
      this.signalr.taskAssigned$.subscribe((evt) => {
        this.tasks.update((list) =>
          list.map((t) => {
            if (t.id === evt.taskId) {
              const assignments = t.assignments.filter(
                (a) => a.assigneeMemberId !== evt.assignment.assigneeMemberId,
              );
              return { ...t, assignments: [...assignments, evt.assignment] };
            }
            return t;
          }),
        );
      }),
    );

    this.subscriptions.add(
      this.signalr.taskArtifactAdded$.subscribe((evt) => {
        const currentDetail = this.selectedTask();
        if (currentDetail && currentDetail.id === evt.taskId) {
          this.selectedTask.set({
            ...currentDetail,
            artifacts: [...currentDetail.artifacts, evt.artifact],
          });
        }
      }),
    );

    this.subscriptions.add(
      this.signalr.taskReviewDecision$.subscribe((evt) => {
        const targetStatus: TaskItemStatus = evt.decision === 'Approved' ? 'Done' : 'InProgress';
        this.tasks.update((list) =>
          list.map((t) => (t.id === evt.taskId ? { ...t, status: targetStatus } : t)),
        );
      }),
    );

    this.subscriptions.add(
      this.signalr.reconnected$.subscribe(() => {
        const channelId = this.activeChannelId();
        if (channelId) {
          void this.loadTasks(channelId);
        }
      }),
    );
  }

  ngOnDestroy(): void {
    this.subscriptions.unsubscribe();
  }
}
