/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * Mô hình dữ liệu đại diện cho một tin nhắn trong kênh làm việc.
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

/**
 * Mô hình dữ liệu thẻ nhắc tên (@mention) thành viên trong tin nhắn.
 */
export interface MessageMention {
  id?: string;
  messageId?: string;
  memberId: string;
  displayName?: string;
}

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
