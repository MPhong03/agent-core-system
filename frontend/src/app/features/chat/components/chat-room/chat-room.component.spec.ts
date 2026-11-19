/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { ChatRoomComponent } from './chat-room.component';
import { ChatStore } from '../../store/chat.store';
import { OrganizationStore } from '../../../organization/store/organization.store';
import { Member, MemberType } from '../../../organization/models/organization.models';
import { ChatMessage } from '../../models/chat.models';

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

    await TestBed.configureTestingModule({
      imports: [ChatRoomComponent],
      providers: [
        { provide: ChatStore, useValue: mockChatStore },
        { provide: OrganizationStore, useValue: mockOrgStore },
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
});
