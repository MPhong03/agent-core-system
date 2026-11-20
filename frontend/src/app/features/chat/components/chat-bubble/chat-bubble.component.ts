/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file chat-bubble.component.ts — Component hiển thị bong bóng tin nhắn đơn lẻ (Sprint 1 - PBI-CHAT-02)
 *
 * ## Giao Diện Bong Bóng Hội Thoại (Chat Bubble)
 * Render nội dung tin nhắn của Người dùng (Human) hoặc AI Agent, hỗ trợ markdown an toàn (code blocks,
 * inline code, bold, italic, lists, @mention tag) và con trỏ streaming token thời gian thực.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05, RULE-06)
 * 🔴 Bảo Mật XSS: Luôn escape ký tự HTML (`&`, `<`, `>`, `"`, `'`) trước khi parse Markdown, sau đó mới bypassSecurityTrustHtml.
 * 🔴 Placeholder Regex: Dùng placeholder không chứa ký tự gạch dưới `_` (`%%%CODEBLOCK0%%%`) để tránh regex italic làm rách token.
 * ⛔ Không gọi parse Markdown trực tiếp trong template interpolation; luôn bọc qua `computed()`.
 * ⇒ `isStreaming = true` kéo theo coi người gửi là AI Agent, hiển thị `typing-cursor` và nhãn đang phản hồi.
 */

import {
  Component,
  ChangeDetectionStrategy,
  computed,
  input,
  inject,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ChatMessage } from '../../models/chat.models';

@Component({
  selector: 'app-chat-bubble',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './chat-bubble.component.html',
  styleUrls: ['./chat-bubble.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChatBubbleComponent {
  // ── Injections ──
  private readonly sanitizer = inject(DomSanitizer);

  // ── Inputs (Angular 19 Signals Input) ──

  /**
   * Tin nhắn cần hiển thị.
   */
  readonly message = input<ChatMessage | null>(null);

  /**
   * Cờ xác định tin nhắn có đang trong quá trình sinh token streaming thời gian thực hay không.
   */
  readonly isStreaming = input<boolean>(false);

  /**
   * Chuỗi token tích lũy tạm thời khi isStreaming = true.
   */
  readonly streamingText = input<string>('');

  // ── Computed Signals (Biến Đổi Trạng Thái Hiển Thị) ──

  /**
   * Xác định người gửi có phải là AI Agent hay không.
   */
  readonly isAgent = computed(() => {
    if (this.isStreaming()) {
      return true;
    }
    return this.message()?.isAgent ?? false;
  });

  /**
   * Tên hiển thị của người gửi tin nhắn.
   */
  readonly senderDisplayName = computed(() => {
    if (this.isStreaming()) {
      return this.message()?.senderDisplayName || 'AI Agent';
    }
    return this.message()?.senderDisplayName || (this.isAgent() ? 'AI Agent' : 'Thành viên');
  });

  /**
   * Nội dung thô của tin nhắn.
   */
  readonly rawText = computed(() => {
    if (this.isStreaming()) {
      return this.streamingText() || '';
    }
    return this.message()?.content || '';
  });

  /**
   * Nội dung tin nhắn đã được parse Markdown và sanitize an toàn chống XSS.
   */
  readonly renderedContent = computed<SafeHtml>(() => {
    const raw = this.rawText();
    if (!raw) {
      return '';
    }

    const html = this.parseSimpleMarkdown(raw);
    return this.sanitizer.bypassSecurityTrustHtml(html);
  });

  /**
   * Thời gian gửi tin nhắn định dạng ngắn gọn.
   */
  readonly formattedTime = computed(() => {
    const dateStr = this.message()?.createdDate;
    if (!dateStr) {
      return '';
    }
    try {
      const d = new Date(dateStr);
      return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return '';
    }
  });

  // ── Bộ Xử Lý Markdown An Toàn (Sanitization) ──

  /**
   * Parser Markdown an toàn hỗ trợ code block, inline code, bold, italic, list và line-breaks.
   */
  private parseSimpleMarkdown(text: string): string {
    // 1. Chống XSS bằng cách escape các ký tự HTML đặc biệt trước tiên
    let escaped = text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');

    // 2. Tách và bảo toàn code block nhiều dòng (triple-backtick) vào placeholder array
    const codeBlocks: string[] = [];
    escaped = escaped.replace(/```(?:([a-zA-Z0-9_-]+)\r?\n)?([\s\S]*?)```/g, (_match, _lang, code) => {
      const index = codeBlocks.length;
      codeBlocks.push(`<pre class="chat-code-block"><code>${code.trim()}</code></pre>`);
      return `%%%CODEBLOCK${index}%%%`;
    });

    // 3. Inline code: `code` (bảo toàn bằng placeholder không chứa ký tự '_')
    const inlineCodes: string[] = [];
    escaped = escaped.replace(/`([^`\n]+)`/g, (_match, code) => {
      const index = inlineCodes.length;
      inlineCodes.push(`<code class="chat-inline-code">${code}</code>`);
      return `%%%INLINECODE${index}%%%`;
    });

    // 4. Bold: **text** hoặc __text__
    escaped = escaped.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');
    escaped = escaped.replace(/__([^_]+)__/g, '<strong>$1</strong>');

    // 5. Italic: *text* hoặc _text_
    escaped = escaped.replace(/\*([^*]+)\*/g, '<em>$1</em>');
    escaped = escaped.replace(/_([^_]+)_/g, '<em>$1</em>');

    // 6. Mentions: @DisplayName (tô sáng thẻ tag)
    escaped = escaped.replace(/(@[\w\p{L}.-]+)/gu, '<span class="chat-mention-tag">$1</span>');

    // 7. Bullet lists: Các dòng bắt đầu bằng `- ` hoặc `* `
    const lines = escaped.split(/\r?\n/);
    const processedLines: string[] = [];
    let inList = false;

    for (const line of lines) {
      const trimmed = line.trim();
      if (trimmed.startsWith('- ') || trimmed.startsWith('* ')) {
        if (!inList) {
          processedLines.push('<ul class="chat-bullet-list">');
          inList = true;
        }
        processedLines.push(`<li>${trimmed.substring(2)}</li>`);
      } else {
        if (inList) {
          processedLines.push('</ul>');
          inList = false;
        }
        processedLines.push(line);
      }
    }

    if (inList) {
      processedLines.push('</ul>');
    }

    // 8. Chuyển đổi newlines sang <br/>
    let result = processedLines.join('\n').replace(/\n/g, '<br/>');

    // 9. Khôi phục inline code và code blocks từ placeholders
    result = result.replace(/%%%INLINECODE(\d+)%%%/g, (_match, idx) => {
      return inlineCodes[Number(idx)] ?? '';
    });

    result = result.replace(/%%%CODEBLOCK(\d+)%%%/g, (_match, idx) => {
      return codeBlocks[Number(idx)] ?? '';
    });

    // 10. Dọn dẹp thẻ <br/> dư thừa quanh block tags
    return result
      .replace(/<br\/><pre/g, '<pre')
      .replace(/<\/pre><br\/>/g, '</pre>')
      .replace(/<br\/><ul/g, '<ul')
      .replace(/<\/ul><br\/>/g, '</ul>');
  }
}
