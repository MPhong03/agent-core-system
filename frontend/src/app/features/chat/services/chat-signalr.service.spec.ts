/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat-signalr.service.spec.ts — Unit tests cho ChatSignalRService (Sprint 1 - PBI-CHAT-01)
 *
 * ## Mục Tiêu Kiểm Thử
 * Đảm bảo các luồng Subject/Observable (tin nhắn hoàn chỉnh, streaming chunk, completion, reconnect)
 * của ChatSignalRService phát đúng dữ liệu và chuyển trạng thái kết nối chuẩn xác.
 */

import { TestBed } from '@angular/core/testing';
import { firstValueFrom } from 'rxjs';
import { ChatSignalRService } from './chat-signalr.service';
import { ChatMessage } from '../models/chat.models';

describe('ChatSignalRService', () => {
  let service: ChatSignalRService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ChatSignalRService],
    });

    service = TestBed.inject(ChatSignalRService);
  });

  afterEach(async () => {
    await service.disconnect();
  });

  it('should be created with initial disconnected state', () => {
    expect(service).toBeTruthy();
    expect(service.isConnected()).toBe(false);
    expect(service.connectionState()).toBe('disconnected');
  });

  it('should expose event observables for messages, chunks, completion, and reconnected', () => {
    expect(service.messageReceived$).toBeDefined();
    expect(service.messageChunkReceived$).toBeDefined();
    expect(service.messageCompleted$).toBeDefined();
    expect(service.reconnected$).toBeDefined();
  });

  it('should dispatch received messages to messageReceived$', async () => {
    const mockMsg: ChatMessage = {
      id: 'm-1',
      channelId: 'ch-1',
      senderMemberId: 'mem-1',
      senderDisplayName: 'User',
      isAgent: false,
      content: 'Testing',
      callDepth: 0,
      parentMessageId: null,
      mentions: [],
      createdDate: new Date().toISOString(),
    };

    const promise = firstValueFrom(service.messageReceived$);

    // Mô phỏng callback nội bộ khi nhận tin nhắn
    (service as any).messageReceivedSubject.next(mockMsg);

    const received = await promise;
    expect(received.id).toBe('m-1');
    expect(received.content).toBe('Testing');
  });

  it('should dispatch received chunks to messageChunkReceived$', async () => {
    const promise = firstValueFrom(service.messageChunkReceived$);

    (service as any).messageChunkSubject.next({
      messageId: 'msg-chunk-1',
      agentMemberId: 'agent-1',
      deltaText: 'Hello',
    });

    const chunk = await promise;
    expect(chunk.messageId).toBe('msg-chunk-1');
    expect(chunk.deltaText).toBe('Hello');
  });

  it('should dispatch completed message to messageCompleted$', async () => {
    const promise = firstValueFrom(service.messageCompleted$);

    (service as any).messageCompletedSubject.next({
      messageId: 'msg-comp-1',
      agentMemberId: 'agent-1',
      agentDisplayName: 'Reviewer Agent',
      fullContent: 'Full Answer',
    });

    const completed = await promise;
    expect(completed.messageId).toBe('msg-comp-1');
    expect(completed.agentMemberId).toBe('agent-1');
    expect(completed.agentDisplayName).toBe('Reviewer Agent');
    expect(completed.fullContent).toBe('Full Answer');
  });

  it('should dispatch reconnected event to reconnected$', async () => {
    const promise = firstValueFrom(service.reconnected$);

    (service as any).reconnectedSubject.next();

    await promise;
  });

  it('should dispatch tool approval request to toolApprovalRequested$', async () => {
    const promise = firstValueFrom(service.toolApprovalRequested$);

    const mockRequest = {
      pendingToolCallId: 'call-uuid-1',
      channelId: 'chan-1',
      chatMessageId: 'msg-1',
      toolCallId: 'tcall-1',
      toolName: 'sql_runner',
      argumentsJson: '{"sql":"SELECT 1"}',
      expiresAt: '2026-11-21T18:00:00Z',
    };

    (service as any).toolApprovalRequestedSubject.next(mockRequest);

    const received = await promise;
    expect(received.pendingToolCallId).toBe('call-uuid-1');
    expect(received.toolName).toBe('sql_runner');
  });

  it('should dispatch tool executing event to toolExecuting$', async () => {
    const promise = firstValueFrom(service.toolExecuting$);

    (service as any).toolExecutingSubject.next({
      channelId: 'chan-1',
      toolName: 'web_search',
    });

    const received = await promise;
    expect(received.channelId).toBe('chan-1');
    expect(received.toolName).toBe('web_search');
  });
});
