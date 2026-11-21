/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat-room.component.spec.ts — Unit tests cho ChatRoomComponent (Sprint 1 & Sprint 3)
 *
 * ## Mục Tiêu Kiểm Thử
 * Đảm bảo ChatRoomComponent hiển thị tiêu đề kênh, số thành viên, tích hợp gửi tin nhắn,
 * điều khiển mention popover, bắt các sự kiện cuộn/bàn phím,
 * và tích hợp thẻ Action Card phê duyệt công cụ (Human-in-the-Loop) & huy hiệu thực thi.
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { Subject, of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ChatRoomComponent } from './chat-room.component';
import { ChatStore } from '../../store/chat.store';
import { OrganizationStore } from '../../../organization/store/organization.store';
import { Member, MemberType } from '../../../organization/models/organization.models';
import {
  ChatMessage,
  CompletedMessage,
  ToolApprovalRequest,
  ToolExecutingEvent,
} from '../../models/chat.models';
import { ChatSignalRService } from '../../services/chat-signalr.service';
import { ChatHttpService } from '../../services/chat-http.service';

describe('ChatRoomComponent', () => {
  let component: ChatRoomComponent;
  let fixture: ComponentFixture<ChatRoomComponent>;

  let mockChatStore: {
    messages: ReturnType<typeof signal<ChatMessage[]>>;
    streamingMessages: ReturnType<typeof signal<Map<string, string>>>;
    activeStreamingEntries: ReturnType<typeof signal<Array<{ messageId: string; content: string }>>>;
    connectionState: ReturnType<typeof signal<string>>;
    isLoadingHistory: ReturnType<typeof signal<boolean>>;
    hasMoreHistory: ReturnType<typeof signal<boolean>>;
    error: ReturnType<typeof signal<string | null>>;
    joinChannel: ReturnType<typeof vi.fn>;
    leaveChannel: ReturnType<typeof vi.fn>;
    sendMessage: ReturnType<typeof vi.fn>;
    loadMoreOlderMessages: ReturnType<typeof vi.fn>;
  };

  let mockOrgStore: {
    channelMembers: ReturnType<typeof signal<Member[]>>;
  };

  let mockChatSignalRService: {
    toolApprovalRequested$: Subject<ToolApprovalRequest>;
    toolExecuting$: Subject<ToolExecutingEvent>;
    messageReceived$: Subject<ChatMessage>;
    messageCompleted$: Subject<CompletedMessage>;
  };

  let mockChatHttpService: {
    confirmToolCall: ReturnType<typeof vi.fn>;
    rejectToolCall: ReturnType<typeof vi.fn>;
  };

  const sampleMembers: Member[] = [
    {
      id: 'mem-1',
      channelId: 'chan-100',
      type: MemberType.Human,
      displayName: 'Alice Human',
      createdDate: '2026-11-19T00:00:00Z',
    },
    {
      id: 'mem-2',
      channelId: 'chan-100',
      type: MemberType.Agent,
      displayName: 'Bot Agent AI',
      createdDate: '2026-11-19T00:00:00Z',
    },
  ];

  const sampleMessages: ChatMessage[] = [
    {
      id: 'msg-1',
      channelId: 'chan-100',
      senderMemberId: 'mem-1',
      senderDisplayName: 'Alice Human',
      isAgent: false,
      content: 'Chào Bot Agent!',
      callDepth: 0,
      parentMessageId: null,
      mentions: [],
      createdDate: '2026-11-19T10:00:00Z',
    },
  ];

  beforeEach(async () => {
    mockChatStore = {
      messages: signal<ChatMessage[]>(sampleMessages),
      streamingMessages: signal(new Map()),
      activeStreamingEntries: signal([]),
      connectionState: signal('connected'),
      isLoadingHistory: signal(false),
      hasMoreHistory: signal(false),
      error: signal(null),
      joinChannel: vi.fn().mockResolvedValue(undefined),
      leaveChannel: vi.fn().mockResolvedValue(undefined),
      sendMessage: vi.fn().mockResolvedValue(undefined),
      loadMoreOlderMessages: vi.fn(),
    };

    mockOrgStore = {
      channelMembers: signal<Member[]>(sampleMembers),
    };

    mockChatSignalRService = {
      toolApprovalRequested$: new Subject<ToolApprovalRequest>(),
      toolExecuting$: new Subject<ToolExecutingEvent>(),
      messageReceived$: new Subject<ChatMessage>(),
      messageCompleted$: new Subject<CompletedMessage>(),
    };

    mockChatHttpService = {
      confirmToolCall: vi.fn().mockReturnValue(of({ success: true, data: true })),
      rejectToolCall: vi.fn().mockReturnValue(of({ success: true, data: true })),
    };

    await TestBed.configureTestingModule({
      imports: [ChatRoomComponent],
      providers: [
        { provide: ChatStore, useValue: mockChatStore },
        { provide: OrganizationStore, useValue: mockOrgStore },
        { provide: ChatSignalRService, useValue: mockChatSignalRService },
        { provide: ChatHttpService, useValue: mockChatHttpService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatRoomComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('channelId', 'chan-100');
    fixture.componentRef.setInput('channelName', 'General');
  });

  it('khởi tạo thành công và tham gia kênh qua ChatStore.joinChannel', () => {
    fixture.detectChanges();
    expect(component).toBeTruthy();
    expect(mockChatStore.joinChannel).toHaveBeenCalledWith('chan-100');
  });

  it('rời kênh qua ChatStore.leaveChannel khi hủy component', () => {
    fixture.detectChanges();
    component.ngOnDestroy();
    expect(mockChatStore.leaveChannel).toHaveBeenCalled();
  });

  it('hiển thị danh sách tin nhắn và trạng thái kết nối', () => {
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;

    expect(compiled.querySelector('.channel-title')?.textContent).toContain('General');
    expect(compiled.querySelector('.connection-pill')?.textContent).toContain('Trực tuyến');
    expect(compiled.querySelectorAll('app-chat-bubble').length).toBe(1);
  });

  it('gửi tin nhắn qua submitMessage và dọn dẹp ô input', async () => {
    fixture.detectChanges();
    component.messageText.set('Tin nhắn thử nghiệm');

    await component.submitMessage();

    expect(mockChatStore.sendMessage).toHaveBeenCalledWith('Tin nhắn thử nghiệm', []);
    expect(component.messageText()).toBe('');
  });

  it('chèn thẻ @mention khi chọn thành viên từ popover', () => {
    fixture.detectChanges();
    component.messageText.set('Chào @Bo');
    component.mentionFilterText.set('Bo');

    // Giả lập bắt đầu mention từ vị trí 5
    (component as any).mentionStartIndex = 5;

    component.onSelectMember(sampleMembers[1]);

    expect(component.messageText()).toBe('Chào @Bot Agent AI ');
    expect(component.selectedMentionIds().has('mem-2')).toBe(true);
    expect(component.isMentionPopoverOpen()).toBe(false);
  });

  it('lắng nghe sự kiện toolApprovalRequested$ và hiển thị app-tool-approval-card', () => {
    fixture.detectChanges();

    const sampleReq: ToolApprovalRequest = {
      pendingToolCallId: '018f2345-6789-7def-8123-456789abcdef',
      channelId: 'chan-100',
      chatMessageId: 'msg-001',
      toolCallId: 'call_123',
      toolName: 'execute_sql_query',
      argumentsJson: '{"limit":10}',
      expiresAt: '2026-11-21T16:00:00Z',
      status: 'pending',
    };

    mockChatSignalRService.toolApprovalRequested$.next(sampleReq);
    fixture.detectChanges();

    expect(component.toolApprovalRequests().length).toBe(1);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-tool-approval-card')).toBeTruthy();
    expect(compiled.textContent).toContain('execute_sql_query');
  });

  it('gọi confirmToolCall khi phê duyệt yêu cầu công cụ', () => {
    fixture.detectChanges();

    const sampleReq: ToolApprovalRequest = {
      pendingToolCallId: '018f2345-6789-7def-8123-456789abcdef',
      channelId: 'chan-100',
      chatMessageId: 'msg-001',
      toolCallId: 'call_123',
      toolName: 'execute_sql_query',
      argumentsJson: '{"limit":10}',
      expiresAt: '2026-11-21T16:00:00Z',
      status: 'pending',
    };

    component.toolApprovalRequests.set([sampleReq]);
    fixture.detectChanges();

    component.confirmToolApproval('018f2345-6789-7def-8123-456789abcdef');

    expect(mockChatHttpService.confirmToolCall).toHaveBeenCalledWith(
      '018f2345-6789-7def-8123-456789abcdef',
    );
    expect(component.toolApprovalRequests()[0].status).toBe('confirmed');
  });

  it('gọi rejectToolCall khi từ chối yêu cầu công cụ', () => {
    fixture.detectChanges();

    const sampleReq: ToolApprovalRequest = {
      pendingToolCallId: '018f2345-6789-7def-8123-456789abcdef',
      channelId: 'chan-100',
      chatMessageId: 'msg-001',
      toolCallId: 'call_123',
      toolName: 'execute_sql_query',
      argumentsJson: '{"limit":10}',
      expiresAt: '2026-11-21T16:00:00Z',
      status: 'pending',
    };

    component.toolApprovalRequests.set([sampleReq]);
    fixture.detectChanges();

    component.rejectToolApproval({
      callId: '018f2345-6789-7def-8123-456789abcdef',
      reason: 'Tham số không an toàn',
    });

    expect(mockChatHttpService.rejectToolCall).toHaveBeenCalledWith(
      '018f2345-6789-7def-8123-456789abcdef',
      'Tham số không an toàn',
    );
    expect(component.toolApprovalRequests()[0].status).toBe('rejected');
  });

  it('hiển thị app-tool-execution-badge khi có sự kiện toolExecuting$', () => {
    fixture.detectChanges();

    mockChatSignalRService.toolExecuting$.next({
      channelId: 'chan-100',
      toolName: 'search_database',
    });
    fixture.detectChanges();

    expect(component.executingTool()?.toolName).toBe('search_database');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-tool-execution-badge')).toBeTruthy();
    expect(compiled.textContent).toContain('search_database');
  });
});
