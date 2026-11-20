/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat.store.ts — Quản lý trạng thái phân hệ Chat thời gian thực bằng Angular Signals (Sprint 1 - PBI-CHAT-01)
 *
 * ## Quản Trị Trạng Thái Realtime Bằng Angular Signals
 * Đóng vai trò Single Source of Truth cho toàn bộ tin nhắn kênh, luồng streaming token real-time,
 * trạng thái kết nối socket SignalR, và phân trang lịch sử tin nhắn cũ.
 *
 * ## Ràng buộc Kỹ thuật & Luồng Dữ liệu
 * 🔴 Streaming Map Bất Biến: `streamingMessages` lưu `Map<string, string>` đại diện cho token tích lũy của từng tin nhắn. Cập nhật qua `new Map(map)` để kích hoạt signal reactivity.
 * ⛔ RULE-06: Cấm nuốt lỗi khi kết nối hoặc nạp tin nhắn; mọi lỗi đều được đẩy vào signal `error`.
 * ⇒ Nhận chunk streaming (`handleIncomingChunk`) ⇒ Cập nhật text tích lũy trong `streamingMessages`.
 * ⇒ Hoàn thành streaming (`handleMessageCompleted`) ⇒ Xóa khỏi `streamingMessages` và chốt vào `messages`.
 * ⇒ Phục hồi kết nối (`reconnected$`) ⇒ Tự động gọi `loadHistory` để bù đắp các tin nhắn bị lỡ.
 */

import { Injectable, inject, signal, computed, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ChatHttpService } from '../services/chat-http.service';
import { ChatSignalRService } from '../services/chat-signalr.service';
import {
  ChatMessage,
  CompletedMessage,
  StreamingChunk,
} from '../models/chat.models';

@Injectable({
  providedIn: 'root',
})
export class ChatStore {
  // ── Injections ──
  private readonly httpService = inject(ChatHttpService);
  private readonly signalrService = inject(ChatSignalRService);
  private readonly destroyRef = inject(DestroyRef);

  // ── State Signals ──
  readonly messages = signal<ChatMessage[]>([]);
  readonly streamingMessages = signal<Map<string, string>>(new Map());
  readonly activeChannelId = signal<string | null>(null);
  readonly isLoadingHistory = signal<boolean>(false);
  readonly hasMoreHistory = signal<boolean>(true);
  readonly error = signal<string | null>(null);

  // ── Delegated SignalR Signals ──
  readonly isConnected = this.signalrService.isConnected;
  readonly connectionState = this.signalrService.connectionState;

  // ── Computed Selectors ──
  readonly activeStreamingEntries = computed(() =>
    Array.from(this.streamingMessages().entries()).map(([messageId, content]) => ({
      messageId,
      content,
    })),
  );
  readonly totalMessages = computed(() => this.messages().length);

  // ── Khởi Tạo Subscriptions ──
  constructor() {
    this.initRealtimeSubscriptions();
  }

  // ── Actions Tham Gia / Rời Kênh ──

  /**
   * Tham gia vào một kênh làm việc: kết nối SignalR, join group và nạp lịch sử tin nhắn.
   *
   * @param channelId Mã định danh kênh làm việc.
   */
  async joinChannel(channelId: string): Promise<void> {
    const current = this.activeChannelId();
    if (current === channelId) {
      return;
    }

    if (current) {
      await this.leaveChannel();
    }

    this.activeChannelId.set(channelId);
    this.messages.set([]);
    this.streamingMessages.set(new Map());
    this.hasMoreHistory.set(true);
    this.error.set(null);

    try {
      await this.signalrService.connect();
      await this.signalrService.joinChannel(channelId);
    } catch (err) {
      console.error('[ChatStore] Lỗi kết nối SignalR khi vào kênh:', err);
      this.error.set('Không thể kết nối tới dịch vụ trò chuyện thời gian thực.');
    }

    this.loadHistory(channelId);
  }

  /**
   * Rời khỏi kênh làm việc hiện tại và dọn dẹp bộ nhớ state.
   */
  async leaveChannel(): Promise<void> {
    const channelId = this.activeChannelId();
    if (!channelId) {
      return;
    }

    try {
      await this.signalrService.leaveChannel(channelId);
    } catch (err) {
      console.warn('[ChatStore] Lỗi khi rời kênh SignalR:', err);
    } finally {
      this.activeChannelId.set(null);
      this.messages.set([]);
      this.streamingMessages.set(new Map());
      this.error.set(null);
    }
  }

  // ── Actions Lịch Sử Tin Nhắn (History) ──

  /**
   * Tải lịch sử tin nhắn của kênh qua REST API (hỗ trợ phân trang lùi qua mốc 'before').
   *
   * @param channelId Mã định danh kênh làm việc.
   * @param limit Số lượng tin nhắn cần lấy (mặc định 50).
   * @param before Mã định danh tin nhắn mốc để lấy các tin nhắn cũ hơn.
   */
  loadHistory(channelId: string, limit = 50, before?: string): void {
    if (this.isLoadingHistory()) {
      return;
    }

    this.isLoadingHistory.set(true);
    this.error.set(null);

    this.httpService.getMessages(channelId, limit, before).subscribe({
      next: (res) => {
        this.isLoadingHistory.set(false);
        if (res.success && res.data) {
          const fetched = res.data;
          this.hasMoreHistory.set(fetched.length >= limit);

          if (before) {
            // Phân trang ngược: Nối các tin nhắn cũ vào đầu danh sách
            this.messages.update((current) => {
              const currentIds = new Set(current.map((m) => m.id));
              const uniqueOlder = fetched.filter((m) => !currentIds.has(m.id));
              return [...uniqueOlder, ...current];
            });
          } else {
            // Tải lần đầu
            this.messages.set(fetched);
          }
        }
      },
      error: (err) => {
        this.isLoadingHistory.set(false);
        console.error('[ChatStore] Lỗi tải lịch sử tin nhắn kênh:', err);
        this.error.set('Không thể tải lịch sử trò chuyện.');
      },
    });
  }

  /**
   * Tải tiếp các tin nhắn cũ hơn trong lịch sử trò chuyện.
   *
   * @param limit Số lượng tin nhắn cần lấy thêm.
   */
  loadMoreOlderMessages(limit = 50): void {
    const channelId = this.activeChannelId();
    const currentMessages = this.messages();

    if (!channelId || currentMessages.length === 0 || !this.hasMoreHistory()) {
      return;
    }

    const oldestMessageId = currentMessages[0].id;
    this.loadHistory(channelId, limit, oldestMessageId);
  }

  // ── Actions Gửi Tin Nhắn (Send Message) ──

  /**
   * Gửi tin nhắn mới vào kênh làm việc hiện tại qua SignalR.
   *
   * @param content Nội dung tin nhắn văn bản.
   * @param mentionIds Danh sách ID các thành viên / Agent được tag @mention.
   */
  async sendMessage(content: string, mentionIds: string[] = []): Promise<void> {
    const channelId = this.activeChannelId();
    if (!channelId) {
      throw new Error('Chưa chọn kênh làm việc để gửi tin nhắn.');
    }

    if (!content || content.trim().length === 0) {
      return;
    }

    await this.signalrService.sendMessage(channelId, content.trim(), mentionIds);
  }

  // ── Internal Handlers (Sự Kiện Realtime) ──

  /**
   * Lắng nghe các luồng sự kiện thời gian thực từ ChatSignalRService.
   */
  private initRealtimeSubscriptions(): void {
    // 1. Lắng nghe tin nhắn mới hoàn chỉnh
    this.signalrService.messageReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((message) => {
        this.handleIncomingMessage(message);
      });

    // 2. Lắng nghe từng chunk token streaming từ AI Agent
    this.signalrService.messageChunkReceived$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((chunk) => {
        this.handleIncomingChunk(chunk);
      });

    // 3. Lắng nghe thông báo AI Agent hoàn tất streaming
    this.signalrService.messageCompleted$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((completed) => {
        this.handleMessageCompleted(completed);
      });

    // 4. Lắng nghe sự kiện phục hồi kết nối SignalR để đồng bộ lại lịch sử tin nhắn
    this.signalrService.reconnected$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const channelId = this.activeChannelId();
        if (channelId) {
          this.loadHistory(channelId);
        }
      });
  }

  /**
   * Xử lý tin nhắn mới đến từ SignalR Hub.
   */
  private handleIncomingMessage(message: ChatMessage): void {
    // Chỉ tiếp nhận tin nhắn thuộc kênh hiện tại
    if (message.channelId !== this.activeChannelId()) {
      return;
    }

    this.messages.update((msgs) => {
      const existingIdx = msgs.findIndex((m) => m.id === message.id);
      if (existingIdx >= 0) {
        const copy = [...msgs];
        copy[existingIdx] = message;
        return copy;
      }
      return [...msgs, message];
    });
  }

  /**
   * Tích lũy chuỗi streaming token theo messageId vào state streamingMessages.
   */
  private handleIncomingChunk(chunk: StreamingChunk): void {
    this.streamingMessages.update((map) => {
      const updatedMap = new Map(map);
      const accumulated = updatedMap.get(chunk.messageId) || '';
      updatedMap.set(chunk.messageId, accumulated + chunk.deltaText);
      return updatedMap;
    });
  }

  /**
   * Đồng bộ và chốt tin nhắn streaming vào danh sách chính thức khi Agent hoàn thành.
   */
  private handleMessageCompleted(completed: CompletedMessage): void {
    // 1. Dọn dẹp cache streaming token
    this.streamingMessages.update((map) => {
      if (!map.has(completed.messageId)) {
        return map;
      }
      const updatedMap = new Map(map);
      updatedMap.delete(completed.messageId);
      return updatedMap;
    });

    // 2. Chốt tin nhắn vào messages chính thức
    this.messages.update((msgs) => {
      const idx = msgs.findIndex((m) => m.id === completed.messageId);
      if (idx >= 0) {
        const copy = [...msgs];
        copy[idx] = {
          ...copy[idx],
          content: completed.fullContent,
        };
        return copy;
      }

      // Tạo đối tượng tin nhắn đầy đủ cho AI Agent
      const agentMessage: ChatMessage = {
        id: completed.messageId,
        channelId: this.activeChannelId() ?? '',
        senderMemberId: completed.agentMemberId,
        senderDisplayName: completed.agentDisplayName || 'AI Agent',
        isAgent: true,
        content: completed.fullContent,
        callDepth: 1,
        parentMessageId: null,
        mentions: [],
        createdDate: new Date().toISOString(),
      };

      return [...msgs, agentMessage];
    });
  }
}
