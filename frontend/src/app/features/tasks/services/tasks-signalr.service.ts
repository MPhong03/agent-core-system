/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tasks-signalr.service.ts — Dịch vụ SignalR thời gian thực kết nối Hub /hubs/tasks (Sprint 2 - PBI-TASK-01)
 *
 * ## Giao Thức WebSocket Thời Gian Thực
 * Quản lý kết nối tới SignalR Tasks Hub (`/hubs/tasks`), lắng nghe các sự kiện cập nhật bảng Kanban,
 * kéo thả thẻ, phân công và nộp sản phẩm đầu ra (Artifacts) của Agent.
 */

import { Injectable, OnDestroy, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  TaskArtifact,
  TaskAssignment,
  TaskItem,
  TaskReviewDecisionEvent,
  TaskStatusChangedEvent,
} from '../models/task.model';

export type SignalRConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';

@Injectable({
  providedIn: 'root',
})
export class TasksSignalRService implements OnDestroy {
  private hubConnection: HubConnection | null = null;
  private connectingPromise: Promise<void> | null = null;
  private currentChannelId: string | null = null;

  // ── State Signals ──
  readonly isConnected = signal<boolean>(false);
  readonly connectionState = signal<SignalRConnectionState>('disconnected');

  // ── Event Streams (RxJS Subjects) ──
  private readonly taskCreatedSubject = new Subject<TaskItem>();
  private readonly taskStatusChangedSubject = new Subject<TaskStatusChangedEvent>();
  private readonly taskAssignedSubject = new Subject<{ taskId: string; assignment: TaskAssignment }>();
  private readonly taskArtifactAddedSubject = new Subject<{ taskId: string; artifact: TaskArtifact }>();
  private readonly taskReviewDecisionSubject = new Subject<TaskReviewDecisionEvent>();
  private readonly reconnectedSubject = new Subject<void>();

  readonly taskCreated$: Observable<TaskItem> = this.taskCreatedSubject.asObservable();
  readonly taskStatusChanged$: Observable<TaskStatusChangedEvent> = this.taskStatusChangedSubject.asObservable();
  readonly taskAssigned$: Observable<{ taskId: string; assignment: TaskAssignment }> = this.taskAssignedSubject.asObservable();
  readonly taskArtifactAdded$: Observable<{ taskId: string; artifact: TaskArtifact }> = this.taskArtifactAddedSubject.asObservable();
  readonly taskReviewDecision$: Observable<TaskReviewDecisionEvent> = this.taskReviewDecisionSubject.asObservable();
  readonly reconnected$: Observable<void> = this.reconnectedSubject.asObservable();

  /**
   * Khởi tạo kết nối tới SignalR Tasks Hub tại endpoint /hubs/tasks.
   */
  async connect(): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      return;
    }

    if (this.connectingPromise) {
      return this.connectingPromise;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl('/hubs/tasks', {
        accessTokenFactory: () => localStorage.getItem('access_token') || '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(environment.production ? LogLevel.Error : LogLevel.Information)
      .build();

    this.registerServerEvents(this.hubConnection);
    this.registerLifecycleHooks(this.hubConnection);

    this.connectionState.set('connecting');

    this.connectingPromise = this.hubConnection
      .start()
      .then(() => {
        this.isConnected.set(true);
        this.connectionState.set('connected');
        this.connectingPromise = null;
      })
      .catch((err) => {
        this.isConnected.set(false);
        this.connectionState.set('disconnected');
        this.connectingPromise = null;
        throw err;
      });

    return this.connectingPromise;
  }

  /**
   * Đăng ký tham gia vào nhóm theo dõi bảng Kanban của kênh.
   */
  async joinChannelTasks(channelId: string): Promise<void> {
    this.currentChannelId = channelId;

    if (!this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      await this.connect();
    }

    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('JoinChannelTasks', channelId);
    }
  }

  /**
   * Rời khỏi nhóm theo dõi bảng Kanban của kênh.
   */
  async leaveChannelTasks(channelId: string): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveChannelTasks', channelId);
    }
    if (this.currentChannelId === channelId) {
      this.currentChannelId = null;
    }
  }

  /**
   * Ngắt kết nối Hub chủ động.
   */
  async disconnect(): Promise<void> {
    if (this.hubConnection) {
      try {
        await this.hubConnection.stop();
      } finally {
        this.hubConnection = null;
        this.isConnected.set(false);
        this.connectionState.set('disconnected');
        this.currentChannelId = null;
      }
    }
  }

  ngOnDestroy(): void {
    void this.disconnect();
  }

  private registerServerEvents(connection: HubConnection): void {
    connection.on('TaskCreated', (task: TaskItem) => {
      this.taskCreatedSubject.next(task);
    });

    connection.on(
      'TaskStatusChanged',
      (taskId: string, oldStatus: string, newStatus: string, actorMemberId: string) => {
        this.taskStatusChangedSubject.next({ taskId, oldStatus, newStatus, actorMemberId });
      },
    );

    connection.on('TaskAssigned', (taskId: string, assignment: TaskAssignment) => {
      this.taskAssignedSubject.next({ taskId, assignment });
    });

    connection.on('TaskArtifactAdded', (taskId: string, artifact: TaskArtifact) => {
      this.taskArtifactAddedSubject.next({ taskId, artifact });
    });

    connection.on(
      'TaskReviewDecision',
      (taskId: string, decision: string, feedback: string | null, reviewerMemberId: string) => {
        this.taskReviewDecisionSubject.next({ taskId, decision, feedback, reviewerMemberId });
      },
    );
  }

  private registerLifecycleHooks(connection: HubConnection): void {
    connection.onreconnecting(() => {
      this.isConnected.set(false);
      this.connectionState.set('reconnecting');
    });

    connection.onreconnected(async () => {
      this.isConnected.set(true);
      this.connectionState.set('connected');

      if (this.currentChannelId) {
        try {
          await connection.invoke('JoinChannelTasks', this.currentChannelId);
        } catch {
          // Bỏ qua lỗi re-join nếu hub chưa sẵn sàng
        }
      }

      this.reconnectedSubject.next();
    });

    connection.onclose(() => {
      this.isConnected.set(false);
      this.connectionState.set('disconnected');
    });
  }
}
