/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach } from 'vitest';
import { ChatBubbleComponent } from './chat-bubble.component';
import { ChatMessage } from '../../models/chat.models';

describe('ChatBubbleComponent', () => {
  let component: ChatBubbleComponent;
  let fixture: ComponentFixture<ChatBubbleComponent>;

  const mockUserMessage: ChatMessage = {
    id: 'msg-1',
    channelId: 'chan-1',
    senderMemberId: 'mem-1',
    senderDisplayName: 'John Doe',
    isAgent: false,
    content: 'Xin chào mọi người! Cùng làm việc nhé.',
    callDepth: 0,
    parentMessageId: null,
    mentions: [],
    createdDate: '2026-11-19T10:00:00Z',
  };

  const mockAgentMessage: ChatMessage = {
    id: 'msg-2',
    channelId: 'chan-1',
    senderMemberId: 'mem-agent-1',
    senderDisplayName: 'Support Bot',
    isAgent: true,
    content: 'Dạ tôi có thể hỗ trợ gì cho bạn? **Đậm** và `code`',
    callDepth: 2,
    parentMessageId: 'msg-1',
    mentions: ['mem-1'],
    createdDate: '2026-11-19T10:01:00Z',
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ChatBubbleComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ChatBubbleComponent);
    component = fixture.componentInstance;
  });

  it('khởi tạo thành công component', () => {
    expect(component).toBeTruthy();
  });

  it('hiển thị đúng thông tin tin nhắn của người dùng (Human)', () => {
    fixture.componentRef.setInput('message', mockUserMessage);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.sender-name')?.textContent).toContain('John Doe');
    expect(compiled.querySelector('.badge-ai')).toBeNull();
    expect(compiled.querySelector('.user-row')).toBeTruthy();
    expect(compiled.querySelector('.bubble-text')?.textContent).toContain('Xin chào mọi người!');
  });

  it('hiển thị đúng huy hiệu AI và CallDepth khi tin nhắn là của AI Agent', () => {
    fixture.componentRef.setInput('message', mockAgentMessage);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.sender-name')?.textContent).toContain('Support Bot');
    expect(compiled.querySelector('.badge-ai')?.textContent).toContain('AI');
    expect(compiled.querySelector('.badge-depth')?.textContent).toContain('Lớp 2');
    expect(compiled.querySelector('.agent-row')).toBeTruthy();
  });

  it('render markdown an toàn (thẻ strong, inline code) và chống XSS', () => {
    const maliciousMsg: ChatMessage = {
      ...mockUserMessage,
      content: 'Chào **bạn**, `const x = 1;` <script>alert(1)</script>',
    };
    fixture.componentRef.setInput('message', maliciousMsg);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const bubbleText = compiled.querySelector('.bubble-text');
    expect(bubbleText?.querySelector('strong')?.textContent).toBe('bạn');
    expect(bubbleText?.querySelector('.chat-inline-code')?.textContent).toBe('const x = 1;');
    expect(bubbleText?.querySelector('script')).toBeNull();
  });

  it('hiển thị trạng thái streaming token với con trỏ nhấp nháy khi isStreaming = true', () => {
    fixture.componentRef.setInput('isStreaming', true);
    fixture.componentRef.setInput('streamingText', 'Đang sinh nội dung phản hồi từ AI...');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.streaming')).toBeTruthy();
    expect(compiled.querySelector('.streaming-pill')?.textContent).toContain('Đang trả lời...');
    expect(compiled.querySelector('.typing-cursor')).toBeTruthy();
    expect(compiled.querySelector('.bubble-text')?.textContent).toContain('Đang sinh nội dung');
  });

  it('bảo toàn nội dung code block nhiều dòng không bị biến đổi bởi formatting markdown khác', () => {
    const codeMsg: ChatMessage = {
      ...mockUserMessage,
      content: 'Trước code:\n```typescript\nconst _private = "*test*";\n@decorator\n```\nSau code.',
    };
    fixture.componentRef.setInput('message', codeMsg);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const pre = compiled.querySelector('.chat-code-block');
    expect(pre).toBeTruthy();
    expect(pre?.querySelector('em')).toBeNull();
    expect(pre?.querySelector('strong')).toBeNull();
    expect(pre?.querySelector('.chat-mention-tag')).toBeNull();
    expect(pre?.textContent).toContain('const _private = "*test*";');
    expect(pre?.textContent).toContain('@decorator');
  });
});
