/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat-signalr.service.ts — Dịch vụ SignalR thời gian thực kết nối Hub /hubs/chat (Sprint 1 - PBI-CHAT-01)
 *
 * ## Giao Thức WebSocket Thời Gian Thực
 * Quản lý kết nối tới SignalR Chat Hub (`/hubs/chat`), điều phối luồng streaming token của AI Agent,
 * nhận tin nhắn mới, quản lý trạng thái kết nối và tự phục hồi khi mạng chập chờn.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-02)
 * 🔴 Auto Reconnect: Cấu hình tái kết nối tự động với các khoảng ngắt quãng [0, 2s, 5s, 10s, 30s].
 * 🔴 Rejoin Channel: Khi SignalR tái kết nối thành công (`onreconnected`), bắt buộc tự động tham gia lại kênh cũ.
 * ⛔ Tránh Duplicate Connection: Lưu giữ `connectingPromise` để ngăn chặn gọi song song `connect()` nhiều lần.
 * ⇒ Luồng sự kiện phân tách rõ: `messageReceived$`, `messageChunkReceived$`, `messageCompleted$`.
 * AI-NOTE: Token xác thực được nạp động từ `localStorage.getItem('access_token')`.
 */

import { Injectable, OnDestroy, signal } from '@angular/core';
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr';
import { Subject, Observable } from 'rxjs';
import {
  ChatMessage,
  CompletedMessage,
  SignalRConnectionState,
  StreamingChunk,
  ToolApprovalRequest,
  ToolExecutingEvent,
} from '../models/chat.models';

@Injectable({
  providedIn: 'root',
})
export class ChatSignalRService implements OnDestroy {
  // ── Khởi Tạo Kết Nối & Trạng Thái Nội Bộ ──
  private hubConnection: HubConnection | null = null;
  private connectingPromise: Promise<void> | null = null;
  private currentChannelId: string | null = null;

  // ── State Signals ──
  readonly isConnected = signal<boolean>(false);
  readonly connectionState = signal<SignalRConnectionState>('disconnected');

  // ── Event Streams (RxJS Subjects) ──
  private readonly messageReceivedSubject = new Subject<ChatMessage>();
  private readonly messageChunkSubject = new Subject<StreamingChunk>();
  private readonly messageCompletedSubject = new Subject<CompletedMessage>();
  private readonly reconnectedSubject = new Subject<void>();
  private readonly toolApprovalRequestedSubject = new Subject<ToolApprovalRequest>();
  private readonly toolExecutingSubject = new Subject<ToolExecutingEvent>();

  readonly messageReceived$: Observable<ChatMessage> = this.messageReceivedSubject.asObservable();
  readonly messageChunkReceived$: Observable<StreamingChunk> =
    this.messageChunkSubject.asObservable();
  readonly messageCompleted$: Observable<CompletedMessage> =
    this.messageCompletedSubject.asObservable();
  readonly reconnected$: Observable<void> = this.reconnectedSubject.asObservable();
  readonly toolApprovalRequested$: Observable<ToolApprovalRequest> =
    this.toolApprovalRequestedSubject.asObservable();
  readonly toolExecuting$: Observable<ToolExecutingEvent> =
    this.toolExecutingSubject.asObservable();

  // ── Khởi Tạo Kết Nối & Quản Lý Vòng Đời Hub ──

  /**
   * Khởi tạo kết nối tới SignalR Chat Hub với cơ chế Auto-Reconnect và Bearer Token.
   */
  async connect(): Promise<void> {
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      this.isConnected.set(true);
      this.connectionState.set('connected');
      return;
    }

    if (this.connectingPromise) {
      return this.connectingPromise;
    }

    if (!this.hubConnection) {
      this.hubConnection = new HubConnectionBuilder()
        .withUrl('/hubs/chat', {
          accessTokenFactory: () => localStorage.getItem('access_token') || '',
        })
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(LogLevel.Information)
        .build();

      this.registerEventHandlers(this.hubConnection);
    }

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
        console.error('[ChatSignalRService] Lỗi khi kết nối SignalR Hub:', err);
        throw err;
      });

    return this.connectingPromise;
  }

  /**
   * Ngắt kết nối SignalR Hub khi không còn sử dụng.
   */
  async disconnect(): Promise<void> {
    if (!this.hubConnection) {
      this.isConnected.set(false);
      this.connectionState.set('disconnected');
      return;
    }

    try {
      if (
        this.hubConnection.state === HubConnectionState.Connected ||
        this.hubConnection.state === HubConnectionState.Connecting ||
        this.hubConnection.state === HubConnectionState.Reconnecting
      ) {
        await this.hubConnection.stop();
      }
    } finally {
      this.isConnected.set(false);
      this.connectionState.set('disconnected');
      this.connectingPromise = null;
    }
  }

  // ── Kênh & Gửi Tin Nhắn (Channel Operations) ──

  /**
   * Tham gia vào nhóm lắng nghe của một kênh làm việc cụ thể trên Hub.
   *
   * @param channelId Mã định danh kênh làm việc.
   */
  async joinChannel(channelId: string): Promise<void> {
    this.currentChannelId = channelId;
    await this.ensureConnected();
    await this.hubConnection!.invoke('JoinChannel', channelId);
  }

  /**
   * Rời khỏi nhóm lắng nghe của một kênh làm việc.
   *
   * @param channelId Mã định danh kênh làm việc.
   */
  async leaveChannel(channelId: string): Promise<void> {
    if (this.currentChannelId === channelId) {
      this.currentChannelId = null;
    }
    if (this.hubConnection?.state === HubConnectionState.Connected) {
      await this.hubConnection.invoke('LeaveChannel', channelId);
    }
  }

  /**
   * Gửi một tin nhắn mới vào kênh làm việc qua SignalR Hub.
   *
   * @param channelId Mã định danh kênh làm việc.
   * @param content Nội dung văn bản tin nhắn.
   * @param mentionIds Danh sách mã định danh thành viên hoặc agent được @mention.
   */
  async sendMessage(
    channelId: string,
    content: string,
    mentionIds: string[] = [],
  ): Promise<void> {
    await this.ensureConnected();
    await this.hubConnection!.invoke('SendMessage', channelId, content, mentionIds);
  }

  private async ensureConnected(): Promise<void> {
    if (this.hubConnection?.state !== HubConnectionState.Connected) {
      await this.connect();
    }
  }

  // ── Đăng Ký Sự Kiện Lắng Nghe Realtime ──

  private registerEventHandlers(connection: HubConnection): void {
    // 1. Nhận tin nhắn mới hoàn chỉnh (từ người dùng hoặc agent)
    connection.on('ReceiveMessage', (message: ChatMessage) => {
      this.messageReceivedSubject.next(message);
    });

    // 2. Nhận từng chunk streaming token từ AI Agent
    connection.on(
      'ReceiveMessageChunk',
      (messageId: string, agentMemberId: string, deltaText: string) => {
        this.messageChunkSubject.next({
          messageId,
          agentMemberId,
          deltaText,
        });
      },
    );

    // 3. Nhận thông báo hoàn tất streaming tin nhắn của AI Agent
    connection.on(
      'ReceiveMessageCompleted',
      (messageId: string, agentMemberId: string, agentDisplayName: string, fullContent: string) => {
        this.messageCompletedSubject.next({
          messageId,
          agentMemberId,
          agentDisplayName,
          fullContent,
        });
      },
    );

    // 4. Nhận yêu cầu phê duyệt công cụ từ AI Agent (Human-in-the-loop Action Card)
    connection.on('ToolApprovalRequested', (request: ToolApprovalRequest) => {
      this.toolApprovalRequestedSubject.next(request);
    });

    // 5. Nhận sự kiện bắt đầu thực thi công cụ
    connection.on('ToolExecuting', (arg1: any, arg2?: any, arg3?: any) => {
      if (typeof arg1 === 'object' && arg1 !== null) {
        this.toolExecutingSubject.next({
          channelId: arg1.channelId ?? this.currentChannelId ?? '',
          toolName: arg1.toolName ?? '',
          toolCallId: arg1.toolCallId,
          argumentsJson: arg1.argumentsJson,
        });
      } else if (typeof arg2 === 'string') {
        this.toolExecutingSubject.next({
          channelId: this.currentChannelId ?? '',
          toolCallId: typeof arg1 === 'string' ? arg1 : undefined,
          toolName: arg2,
          argumentsJson: typeof arg3 === 'string' ? arg3 : undefined,
        });
      } else if (typeof arg1 === 'string') {
        this.toolExecutingSubject.next({
          channelId: this.currentChannelId ?? '',
          toolName: arg1,
        });
      }
    });

    // Xử lý các trạng thái vòng đời kết nối socket
    connection.onreconnecting((error) => {
      console.warn('[ChatSignalRService] Đang thử kết nối lại SignalR Hub...', error);
      this.isConnected.set(false);
      this.connectionState.set('reconnecting');
    });

    connection.onreconnected((connectionId) => {
      console.info('[ChatSignalRService] Đã kết nối lại SignalR Hub thành công:', connectionId);
      this.isConnected.set(true);
      this.connectionState.set('connected');

      // Tự động tái tham gia group kênh nếu trước đó đang ở trong kênh
      if (this.currentChannelId) {
        this.joinChannel(this.currentChannelId).catch((err) =>
          console.error('[ChatSignalRService] Lỗi khi tái tham gia kênh sau kết nối lại:', err),
        );
      }

      this.reconnectedSubject.next();
    });

    connection.onclose((error) => {
      if (error) {
        console.error('[ChatSignalRService] Kết nối SignalR Hub đã bị đóng do lỗi:', error);
      }
      this.isConnected.set(false);
      this.connectionState.set('disconnected');
    });
  }

  // ── Dọn Dẹp Bộ Nhớ (ngOnDestroy) ──

  ngOnDestroy(): void {
    this.messageReceivedSubject.complete();
    this.messageChunkSubject.complete();
    this.messageCompletedSubject.complete();
    this.reconnectedSubject.complete();
    this.toolApprovalRequestedSubject.complete();
    this.toolExecutingSubject.complete();
    this.disconnect().catch((err) =>
      console.warn('[ChatSignalRService] Lỗi khi hủy kết nối on destroy:', err),
    );
  }
}
