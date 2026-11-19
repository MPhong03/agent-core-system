/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
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
} from '../models/chat.models';

/**
 * Dịch vụ giao tiếp thời gian thực SignalR cho kênh Chat.
 *
 * <remarks>
 * <para><strong>What:</strong> Quản lý kết nối WebSocket/SignalR tới Hub /hubs/chat, tự động tái kết nối, tham gia/rời nhóm kênh và điều phối các sự kiện tin nhắn thời gian thực.</para>
 * <para><strong>When:</strong> Được kích hoạt khi người dùng mở giao diện trò chuyện kênh hoặc cần nhận luồng token streaming từ AI Agents.</para>
 * <para><strong>Why:</strong> Đảm bảo độ trễ thấp nhất cho trải nghiệm hội thoại, xử lý mượt mà việc stream từng token và tự phục hồi kết nối khi mạng chập chờn.</para>
 * </remarks>
 */
@Injectable({
  providedIn: 'root',
})
export class ChatSignalRService implements OnDestroy {
  private hubConnection: HubConnection | null = null;
  private connectingPromise: Promise<void> | null = null;
  private currentChannelId: string | null = null;

  // Reactive State Signals
  readonly isConnected = signal<boolean>(false);
  readonly connectionState = signal<SignalRConnectionState>('disconnected');

  // Reactive Event Streams
  private readonly messageReceivedSubject = new Subject<ChatMessage>();
  private readonly messageChunkSubject = new Subject<StreamingChunk>();
  private readonly messageCompletedSubject = new Subject<CompletedMessage>();
  private readonly reconnectedSubject = new Subject<void>();

  readonly messageReceived$: Observable<ChatMessage> = this.messageReceivedSubject.asObservable();
  readonly messageChunkReceived$: Observable<StreamingChunk> =
    this.messageChunkSubject.asObservable();
  readonly messageCompleted$: Observable<CompletedMessage> =
    this.messageCompletedSubject.asObservable();
  readonly reconnected$: Observable<void> = this.reconnectedSubject.asObservable();

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

  /**
   * Tham gia vào nhóm lắng nghe của một kênh làm việc cụ thể.
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

  private registerEventHandlers(connection: HubConnection): void {
    // 1. Nhận tin nhắn mới hoàn chỉnh (từ người dùng hoặc agent)
    connection.on('ReceiveMessage', (message: ChatMessage) => {
      this.messageReceivedSubject.next(message);
    });

    // 2. Nhận từng chunk streaming từ AI Agent
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

    // Xử lý các vòng đời kết nối
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

  ngOnDestroy(): void {
    this.messageReceivedSubject.complete();
    this.messageChunkSubject.complete();
    this.messageCompletedSubject.complete();
    this.reconnectedSubject.complete();
    this.disconnect().catch((err) =>
      console.warn('[ChatSignalRService] Lỗi khi hủy kết nối on destroy:', err),
    );
  }
}
