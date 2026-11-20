/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat.store.spec.ts — Unit tests cho ChatStore (Sprint 1 - PBI-CHAT-01)
 *
 * ## Mục Tiêu Kiểm Thử
 * Đảm bảo ChatStore quản lý chính xác trạng thái tin nhắn kênh, ghép nối streaming token từ SignalR,
 * chốt tin nhắn khi Agent hoàn thành và dọn dẹp state khi rời kênh.
 */

import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of, Subject } from 'rxjs';
import { vi, describe, it, expect, beforeEach } from 'vitest';
import { ChatStore } from './chat.store';
import { ChatHttpService } from '../services/chat-http.service';
import { ChatSignalRService } from '../services/chat-signalr.service';
import { ChatMessage, CompletedMessage, StreamingChunk } from '../models/chat.models';
import { ApiResponse } from '../../../core/models/api-response.model';

describe('ChatStore', () => {
  let store: ChatStore;
  let mockHttpService: { getMessages: ReturnType<typeof vi.fn> };
  let mockSignalRService: {
    isConnected: any;
    connectionState: any;
    messageReceived$: any;
    messageChunkReceived$: any;
    messageCompleted$: any;
    reconnected$: any;
    messageReceivedSubject: Subject<ChatMessage>;
    messageChunkSubject: Subject<StreamingChunk>;
    messageCompletedSubject: Subject<CompletedMessage>;
    reconnectedSubject: Subject<void>;
    connect: ReturnType<typeof vi.fn>;
    disconnect: ReturnType<typeof vi.fn>;
    joinChannel: ReturnType<typeof vi.fn>;
    leaveChannel: ReturnType<typeof vi.fn>;
    sendMessage: ReturnType<typeof vi.fn>;
  };

  const mockChannelId = 'ch-uuid-123';
  const initialMessages: ChatMessage[] = [
    {
      id: 'msg-1',
      channelId: mockChannelId,
      senderMemberId: 'mem-1',
      senderDisplayName: 'John Doe',
      isAgent: false,
      content: 'Message 1',
      callDepth: 0,
      parentMessageId: null,
      mentions: [],
      createdDate: '2026-11-19T10:00:00Z',
    },
    {
      id: 'msg-2',
      channelId: mockChannelId,
      senderMemberId: 'mem-2',
      senderDisplayName: 'Agent Smith',
      isAgent: true,
      content: 'Message 2',
      callDepth: 1,
      parentMessageId: 'msg-1',
      mentions: [],
      createdDate: '2026-11-19T10:01:00Z',
    },
  ];

  beforeEach(() => {
    mockHttpService = {
      getMessages: vi.fn().mockReturnValue(
        of({
          success: true,
          message: 'OK',
          data: initialMessages,
          timestamp: '2026-11-19T10:00:00Z',
        } as ApiResponse<ChatMessage[]>),
      ),
    };

    const messageReceivedSubject = new Subject<ChatMessage>();
    const messageChunkSubject = new Subject<StreamingChunk>();
    const messageCompletedSubject = new Subject<CompletedMessage>();
    const reconnectedSubject = new Subject<void>();

    const isConnectedSignal = signal(false);
    const connectionStateSignal = signal('disconnected');

    mockSignalRService = {
      isConnected: isConnectedSignal,
      connectionState: connectionStateSignal,
      messageReceived$: messageReceivedSubject.asObservable(),
      messageChunkReceived$: messageChunkSubject.asObservable(),
      messageCompleted$: messageCompletedSubject.asObservable(),
      reconnected$: reconnectedSubject.asObservable(),
      messageReceivedSubject,
      messageChunkSubject,
      messageCompletedSubject,
      reconnectedSubject,
      connect: vi.fn().mockResolvedValue(undefined),
      disconnect: vi.fn().mockResolvedValue(undefined),
      joinChannel: vi.fn().mockResolvedValue(undefined),
      leaveChannel: vi.fn().mockResolvedValue(undefined),
      sendMessage: vi.fn().mockResolvedValue(undefined),
    };

    TestBed.configureTestingModule({
      providers: [
        ChatStore,
        { provide: ChatHttpService, useValue: mockHttpService },
        { provide: ChatSignalRService, useValue: mockSignalRService },
      ],
    });

    store = TestBed.inject(ChatStore);
  });

  it('should initialize with default empty state', () => {
    expect(store.messages()).toEqual([]);
    expect(store.streamingMessages().size).toBe(0);
    expect(store.activeChannelId()).toBeNull();
    expect(store.isLoadingHistory()).toBe(false);
    expect(store.hasMoreHistory()).toBe(true);
  });

  it('should joinChannel, connect SignalR, and load initial history', async () => {
    await store.joinChannel(mockChannelId);

    expect(store.activeChannelId()).toBe(mockChannelId);
    expect(mockSignalRService.connect).toHaveBeenCalled();
    expect(mockSignalRService.joinChannel).toHaveBeenCalledWith(mockChannelId);
    expect(mockHttpService.getMessages).toHaveBeenCalledWith(mockChannelId, 50, undefined);
    expect(store.messages().length).toBe(2);
    expect(store.totalMessages()).toBe(2);
  });

  it('should load history and prepend older messages with before parameter', async () => {
    await store.joinChannel(mockChannelId);

    const olderMessages: ChatMessage[] = [
      {
        id: 'msg-0',
        channelId: mockChannelId,
        senderMemberId: 'mem-1',
        senderDisplayName: 'John Doe',
        isAgent: false,
        content: 'Older message',
        callDepth: 0,
        parentMessageId: null,
        mentions: [],
        createdDate: '2026-11-19T09:59:00Z',
      },
    ];

    mockHttpService.getMessages.mockReturnValue(
      of({
        success: true,
        message: 'OK',
        data: olderMessages,
        timestamp: '2026-11-19T09:59:00Z',
      } as ApiResponse<ChatMessage[]>),
    );

    store.loadHistory(mockChannelId, 50, 'msg-1');

    expect(store.messages().length).toBe(3);
    expect(store.messages()[0].id).toBe('msg-0');
    expect(store.messages()[1].id).toBe('msg-1');
  });

  it('should handle incoming new message via SignalR', async () => {
    await store.joinChannel(mockChannelId);

    const newMsg: ChatMessage = {
      id: 'msg-3',
      channelId: mockChannelId,
      senderMemberId: 'mem-3',
      senderDisplayName: 'Alice',
      isAgent: false,
      content: 'Hello everyone!',
      callDepth: 0,
      parentMessageId: null,
      mentions: [],
      createdDate: '2026-11-19T10:02:00Z',
    };

    mockSignalRService.messageReceivedSubject.next(newMsg);

    expect(store.messages().length).toBe(3);
    expect(store.messages()[2].id).toBe('msg-3');
    expect(store.messages()[2].content).toBe('Hello everyone!');
  });

  it('should ignore incoming message for a different channel', async () => {
    await store.joinChannel(mockChannelId);

    const otherChannelMsg: ChatMessage = {
      id: 'msg-other',
      channelId: 'different-ch-999',
      senderMemberId: 'mem-1',
      senderDisplayName: 'Other',
      isAgent: false,
      content: 'Wrong channel',
      callDepth: 0,
      parentMessageId: null,
      mentions: [],
      createdDate: '2026-11-19T10:02:00Z',
    };

    mockSignalRService.messageReceivedSubject.next(otherChannelMsg);

    expect(store.messages().length).toBe(2);
  });

  it('should accumulate streaming chunks smoothly by messageId', async () => {
    await store.joinChannel(mockChannelId);

    const chunk1: StreamingChunk = {
      messageId: 'agent-msg-stream-1',
      agentMemberId: 'agent-member-1',
      deltaText: 'Xin chào',
    };
    const chunk2: StreamingChunk = {
      messageId: 'agent-msg-stream-1',
      agentMemberId: 'agent-member-1',
      deltaText: ', tôi là AI.',
    };

    mockSignalRService.messageChunkSubject.next(chunk1);
    expect(store.streamingMessages().get('agent-msg-stream-1')).toBe('Xin chào');

    mockSignalRService.messageChunkSubject.next(chunk2);
    expect(store.streamingMessages().get('agent-msg-stream-1')).toBe('Xin chào, tôi là AI.');
    expect(store.activeStreamingEntries().length).toBe(1);
    expect(store.activeStreamingEntries()[0].content).toBe('Xin chào, tôi là AI.');
  });

  it('should finalize streaming message upon ReceiveMessageCompleted', async () => {
    await store.joinChannel(mockChannelId);

    // Stream chunk
    mockSignalRService.messageChunkSubject.next({
      messageId: 'agent-msg-stream-1',
      agentMemberId: 'agent-member-1',
      deltaText: 'Đang suy nghĩ...',
    });
    expect(store.streamingMessages().has('agent-msg-stream-1')).toBe(true);

    // Complete
    const completed: CompletedMessage = {
      messageId: 'agent-msg-stream-1',
      agentMemberId: 'agent-member-1',
      agentDisplayName: 'Reviewer Agent',
      fullContent: 'Hoàn thành câu trả lời phân tích.',
    };
    mockSignalRService.messageCompletedSubject.next(completed);

    // Streaming cache removed
    expect(store.streamingMessages().has('agent-msg-stream-1')).toBe(false);

    // Message added to store messages
    const lastMsg = store.messages()[store.messages().length - 1];
    expect(lastMsg.id).toBe('agent-msg-stream-1');
    expect(lastMsg.content).toBe('Hoàn thành câu trả lời phân tích.');
    expect(lastMsg.senderDisplayName).toBe('Reviewer Agent');
    expect(lastMsg.isAgent).toBe(true);
  });

  it('should resync history upon reconnected$ signal', async () => {
    await store.joinChannel(mockChannelId);
    expect(mockHttpService.getMessages).toHaveBeenCalledTimes(1);

    mockSignalRService.reconnectedSubject.next();

    expect(mockHttpService.getMessages).toHaveBeenCalledTimes(2);
  });

  it('should send message via SignalR service', async () => {
    await store.joinChannel(mockChannelId);

    await store.sendMessage('Hello Agent!', ['agent-id-1']);

    expect(mockSignalRService.sendMessage).toHaveBeenCalledWith(
      mockChannelId,
      'Hello Agent!',
      ['agent-id-1'],
    );
  });

  it('should leaveChannel and reset state', async () => {
    await store.joinChannel(mockChannelId);
    expect(store.activeChannelId()).toBe(mockChannelId);

    await store.leaveChannel();

    expect(store.activeChannelId()).toBeNull();
    expect(store.messages()).toEqual([]);
    expect(store.streamingMessages().size).toBe(0);
    expect(mockSignalRService.leaveChannel).toHaveBeenCalledWith(mockChannelId);
  });
});
