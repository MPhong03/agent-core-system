/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat.models.ts — Định nghĩa kiểu dữ liệu & interfaces cho phân hệ Chat thời gian thực (Sprint 1 - PBI-CHAT-01)
 *
 * ## Cấu trúc Dữ liệu Phân Hệ Chat
 * Phản ánh mô hình thông điệp thời gian thực (SignalR Hub `/hubs/chat`) và lịch sử tin nhắn REST API.
 *
 * ## Ràng buộc Kỹ thuật & Khớp Hợp đồng FE ↔ BE (RULE-01)
 * 🔴 UUIDv7: Tất cả khóa chính `id`, `channelId`, `senderMemberId`, `messageId` đều là chuỗi `string` (UUIDv7 từ .NET Guid).
 * 🔴 Streaming Protocol: Luồng streaming token được gửi qua sự kiện `ReceiveMessageChunk` với `deltaText` và kết thúc bằng `ReceiveMessageCompleted`.
 * ⛔ Cấm tự ý đổi tên event SignalR khác với Hub Backend (`ReceiveMessage`, `ReceiveMessageChunk`, `ReceiveMessageCompleted`).
 * ⇒ `callDepth`: Thể hiện độ sâu của chuỗi đàm thoại giữa các Agent (Agent Gọi Agent), dùng để hiển thị badge lớp xử lý.
 */

// ── Models Tin Nhắn Kênh & Streaming ──

/**
 * Mô hình dữ liệu đại diện cho một tin nhắn chính thức trong kênh làm việc.
 */
export interface ChatMessage {
  id: string;
  channelId: string;
  senderMemberId: string;
  senderDisplayName: string;
  isAgent: boolean;
  content: string;
  callDepth: number;
  parentMessageId: string | null;
  mentions: string[];
  createdDate: string;
}

/**
 * Dữ liệu gói tin streaming token gửi từ AI Agent qua SignalR.
 */
export interface StreamingChunk {
  messageId: string;
  agentMemberId: string;
  deltaText: string;
}

/**
 * Dữ liệu sự kiện khi AI Agent hoàn thành quá trình sinh văn bản và phản hồi tin nhắn.
 */
export interface CompletedMessage {
  messageId: string;
  agentMemberId: string;
  agentDisplayName: string;
  fullContent: string;
}

// ── Models Thẻ Nhắc Tên (@mention) ──

/**
 * Mô hình dữ liệu thẻ nhắc tên (@mention) thành viên trong tin nhắn.
 */
export interface MessageMention {
  id?: string;
  messageId?: string;
  memberId: string;
  displayName?: string;
}

// ── DTO Requests & Trạng Thái Kết Nối ──

/**
 * Dữ liệu yêu cầu gửi tin nhắn mới vào kênh làm việc.
 */
export interface SendChatMessageRequest {
  channelId: string;
  content: string;
  mentionIds?: string[];
}

/**
 * Trạng thái kết nối SignalR Hub thời gian thực.
 */
export type SignalRConnectionState = 'disconnected' | 'connecting' | 'connected' | 'reconnecting';
