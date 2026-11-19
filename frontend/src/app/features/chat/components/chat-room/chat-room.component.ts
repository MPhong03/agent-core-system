/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import {
  Component,
  ChangeDetectionStrategy,
  inject,
  input,
  signal,
  computed,
  effect,
  ViewChild,
  ElementRef,
  OnDestroy,
  AfterViewInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ChatStore } from '../../store/chat.store';
import { OrganizationStore } from '../../../organization/store/organization.store';
import { Member, MemberType } from '../../../organization/models/organization.models';
import { ChatBubbleComponent } from '../chat-bubble/chat-bubble.component';
import { MentionPopoverComponent } from '../mention-popover/mention-popover.component';

/**
 * Phòng chat thời gian thực cho kênh làm việc nhóm (Chat Room).
 *
 * <remarks>
 * <para><strong>What:</strong> Giao diện hội thoại kênh hoàn chỉnh tích hợp danh sách tin nhắn, streaming token real-time từ AI Agent, nhập tin nhắn kèm autocomplete @mention.</para>
 * <para><strong>When:</strong> Được hiển thị khi người dùng chọn một kênh nhóm (TeamChannel) trong cấu trúc phòng ban.</para>
 * <para><strong>Why:</strong> Tạo không gian làm việc cộng tác liền mạch giữa nhân sự thật và các biệt đội AI Agent, hỗ trợ đàm thoại đa tầng và streaming tức thì.</para>
 * </remarks>
 */
@Component({
  selector: 'app-chat-room',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ChatBubbleComponent,
    MentionPopoverComponent,
  ],
  templateUrl: './chat-room.component.html',
  styleUrls: ['./chat-room.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatRoomComponent implements AfterViewInit, OnDestroy {
  readonly chatStore = inject(ChatStore);
  readonly orgStore = inject(OrganizationStore);

  @ViewChild('messagesContainer') private readonly messagesContainerRef?: ElementRef<HTMLDivElement>;
  @ViewChild('messageTextarea') private readonly messageTextareaRef?: ElementRef<HTMLTextAreaElement>;
  @ViewChild(MentionPopoverComponent) private readonly mentionPopover?: MentionPopoverComponent;

  /**
   * Mã định danh kênh làm việc hiện tại.
   */
  readonly channelId = input.required<string>();

  /**
   * Tên kênh làm việc để hiển thị tiêu đề.
   */
  readonly channelName = input<string>('');

  // Local UI State Signals
  readonly messageText = signal<string>('');
  readonly isSending = signal<boolean>(false);
  readonly isMentionPopoverOpen = signal<boolean>(false);
  readonly mentionFilterText = signal<string>('');
  readonly selectedMentionIds = signal<Set<string>>(new Set());

  // Scroll tracking
  private isUserScrolledUp = false;
  private scrollRafId: number | null = null;
  readonly showScrollBottomButton = signal<boolean>(false);

  // Vị trí con trỏ bắt đầu của ký tự '@' hiện tại trong ô input
  private mentionStartIndex = -1;

  /**
   * Danh sách thành viên khả dụng trong kênh để gợi ý @mention.
   */
  readonly channelMembers = computed<Member[]>(() => {
    return this.orgStore.channelMembers();
  });

  /**
   * Trạng thái kết nối hiển thị cho người dùng.
   */
  readonly connectionStatusLabel = computed<string>(() => {
    switch (this.chatStore.connectionState()) {
      case 'connected':
        return 'Trực tuyến';
      case 'connecting':
        return 'Đang kết nối...';
      case 'reconnecting':
        return 'Đang kết nối lại...';
      case 'disconnected':
      default:
        return 'Mất kết nối';
    }
  });

  constructor() {
    // 1. Quản lý việc tham gia / chuyển kênh SignalR
    effect(() => {
      const targetChannelId = this.channelId();
      if (targetChannelId) {
        this.chatStore.joinChannel(targetChannelId);
      }
    });

    // 2. Tự động cuộn xuống cuối khi có tin nhắn mới hoặc streaming token (throttled bằng requestAnimationFrame)
    effect(() => {
      // Đăng ký phụ thuộc tín hiệu
      this.chatStore.messages();
      this.chatStore.activeStreamingEntries();

      if (!this.isUserScrolledUp) {
        if (this.scrollRafId !== null) {
          cancelAnimationFrame(this.scrollRafId);
        }
        this.scrollRafId = requestAnimationFrame(() => {
          this.scrollToBottom();
          this.scrollRafId = null;
        });
      }
    });
  }

  ngAfterViewInit(): void {
    this.scrollToBottom();
  }

  ngOnDestroy(): void {
    if (this.scrollRafId !== null) {
      cancelAnimationFrame(this.scrollRafId);
      this.scrollRafId = null;
    }
    this.chatStore.leaveChannel();
  }

  /**
   * Xử lý sự kiện cuộn của khung tin nhắn để phát hiện người dùng có đang cuộn lên đọc tin cũ hay không.
   */
  onScroll(): void {
    const el = this.messagesContainerRef?.nativeElement;
    if (!el) {
      return;
    }

    const distanceToBottom = el.scrollHeight - el.scrollTop - el.clientHeight;
    this.isUserScrolledUp = distanceToBottom > 80;
    this.showScrollBottomButton.set(this.isUserScrolledUp);
  }

  /**
   * Cuộn mượt khung tin nhắn xuống đáy cùng.
   */
  scrollToBottom(smooth = false): void {
    const el = this.messagesContainerRef?.nativeElement;
    if (!el) {
      return;
    }

    if (smooth) {
      el.scrollTo({ top: el.scrollHeight, behavior: 'smooth' });
    } else {
      el.scrollTop = el.scrollHeight;
    }

    this.isUserScrolledUp = false;
    this.showScrollBottomButton.set(false);
  }

  /**
   * Tải thêm tin nhắn cũ hơn trong lịch sử trò chuyện.
   */
  loadMoreOlderMessages(): void {
    this.chatStore.loadMoreOlderMessages();
  }

  /**
   * Bắt các phím đặc biệt trong ô nhập văn bản (Enter để gửi, phím điều hướng @mention popover).
   */
  onKeyDown(event: KeyboardEvent): void {
    // Nếu popover nhắc tên đang mở, ưu tiên cho popover xử lý các phím Arrow/Enter/Escape
    if (this.isMentionPopoverOpen() && this.mentionPopover) {
      const handled = this.mentionPopover.handleKeyDown(event);
      if (handled) {
        return;
      }
    }

    // Nhấn Enter không kèm Shift để gửi tin nhắn
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.submitMessage();
    }
  }

  /**
   * Theo dõi thay đổi nội dung ô nhập để phát hiện ký tự '@' và kích hoạt gợi ý thành viên.
   */
  onInputChanged(event: Event): void {
    const textarea = event.target as HTMLTextAreaElement;
    const text = textarea.value;
    const cursorPos = textarea.selectionStart ?? text.length;

    const textBeforeCursor = text.substring(0, cursorPos);
    const mentionMatch = /(@[\w\p{L}.-]*)$/u.exec(textBeforeCursor);

    if (mentionMatch) {
      const fullMatch = mentionMatch[1];
      this.mentionStartIndex = cursorPos - fullMatch.length;
      const query = fullMatch.substring(1); // bỏ ký tự '@'
      this.mentionFilterText.set(query);
      this.isMentionPopoverOpen.set(true);
    } else {
      this.closeMentionPopover();
    }
  }

  /**
   * Xử lý khi người dùng chọn một thành viên từ popover gợi ý.
   */
  onSelectMember(member: Member): void {
    const textarea = this.messageTextareaRef?.nativeElement;
    const currentText = this.messageText();

    if (this.mentionStartIndex >= 0 && textarea) {
      let cursorPos = textarea.selectionStart ?? currentText.length;
      if (cursorPos <= this.mentionStartIndex) {
        cursorPos = currentText.length;
      }
      const beforeMention = currentText.substring(0, this.mentionStartIndex);
      const afterCursor = currentText.substring(cursorPos);

      const replacement = `@${member.displayName} `;
      const newText = `${beforeMention}${replacement}${afterCursor}`;

      this.messageText.set(newText);
      this.selectedMentionIds.update((set) => {
        const next = new Set(set);
        next.add(member.id);
        return next;
      });

      this.closeMentionPopover();

      // Đặt lại con trỏ chuột ngay sau chuỗi @mention vừa chèn
      const newCursorPos = beforeMention.length + replacement.length;
      setTimeout(() => {
        textarea.focus();
        textarea.setSelectionRange(newCursorPos, newCursorPos);
      }, 0);
    } else {
      this.closeMentionPopover();
    }
  }

  /**
   * Đóng popover gợi ý nhắc tên.
   */
  closeMentionPopover(): void {
    this.isMentionPopoverOpen.set(false);
    this.mentionFilterText.set('');
    this.mentionStartIndex = -1;
  }

  /**
   * Gửi tin nhắn vào kênh làm việc qua ChatStore.
   */
  async submitMessage(): Promise<void> {
    const text = this.messageText().trim();
    if (!text || this.isSending()) {
      return;
    }

    this.isSending.set(true);
    const mentionIds = Array.from(this.selectedMentionIds());

    try {
      await this.chatStore.sendMessage(text, mentionIds);
      this.messageText.set('');
      this.selectedMentionIds.set(new Set());
      this.closeMentionPopover();

      // Cuộn xuống đáy sau khi gửi
      setTimeout(() => this.scrollToBottom(true), 50);
    } catch (err) {
      console.error('[ChatRoomComponent] Lỗi khi gửi tin nhắn:', err);
    } finally {
      this.isSending.set(false);
    }
  }
}
