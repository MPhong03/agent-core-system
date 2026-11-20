/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file mention-popover.component.ts — Popover gợi ý và autocomplete nhắc tên (@mention) (Sprint 1 - PBI-CHAT-03)
 *
 * ## Giao Diện Autocomplete Nhắc Tên Thành Viên
 * Hiển thị danh sách gợi ý thành viên (Người dùng & AI Agent) khi gõ ký tự '@' trong ô nhập tin nhắn.
 * Hỗ trợ lọc danh sách theo chuỗi tìm kiếm, duyệt danh sách bằng phím mũi tên Lên/Xuống,
 * nhấn Enter/Tab để chèn và Escape để đóng.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05)
 * 🔴 Keyboard Handling: Chặn sự kiện mặc định (`preventDefault`) của ArrowUp/Down/Enter/Tab để ngăn trỏ chuột nhảy lung tung trong textarea.
 * ⛔ Cấm sửa đổi trực tiếp mảng `members`; toàn bộ việc lọc được thực hiện bất biến qua `computed()`.
 * ⇒ Khi từ khóa lọc thay đổi ⇒ Tự động reset chỉ số được chọn `selectedIndex` về `0`.
 * AI-NOTE: Phân định rõ ràng giữa Agent AI và Con người thông qua icon và nhãn badge.
 */

import {
  Component,
  ChangeDetectionStrategy,
  computed,
  input,
  output,
  signal,
  effect,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { Member, MemberType } from '../../../organization/models/organization.models';

@Component({
  selector: 'app-mention-popover',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mention-popover.component.html',
  styleUrls: ['./mention-popover.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MentionPopoverComponent {
  readonly MemberType = MemberType;

  // ── Inputs & Outputs (Angular 19 Signals) ──

  /**
   * Danh sách toàn bộ thành viên trong kênh làm việc.
   */
  readonly members = input<Member[]>([]);

  /**
   * Từ khóa tìm kiếm sau ký tự '@' (ví dụ: gõ '@ai' -> filterText = 'ai').
   */
  readonly filterText = input<string>('');

  /**
   * Trạng thái hiển thị của popover.
   */
  readonly visible = input<boolean>(false);

  /**
   * Sự kiện khi người dùng chọn một thành viên từ danh sách.
   */
  readonly selectMember = output<Member>();

  /**
   * Sự kiện yêu cầu đóng popover (khi bấm Escape hoặc click ngoài).
   */
  readonly closePopover = output<void>();

  // ── State Signals ──

  /**
   * Chỉ số thành viên đang được chọn (active highlight) bằng bàn phím.
   */
  readonly selectedIndex = signal<number>(0);

  // ── Computed Signals (Lọc Thành Viên) ──

  /**
   * Danh sách thành viên đã được lọc theo filterText.
   */
  readonly filteredMembers = computed<Member[]>(() => {
    const list = this.members();
    const query = this.filterText().trim().toLowerCase();

    if (!query) {
      return list;
    }

    return list.filter((m) => m.displayName.toLowerCase().includes(query));
  });

  // ── Lifecycle & Effects ──
  constructor() {
    // Tự động reset con trỏ chọn về vị trí đầu tiên khi danh sách lọc thay đổi
    effect(() => {
      this.filteredMembers();
      this.selectedIndex.set(0);
    });
  }

  // ── Điều Hướng Bàn Phím & Lựa Chọn ──

  /**
   * Xử lý các phím điều hướng bàn phím (Mũi tên Lên/Xuống, Enter, Escape).
   *
   * @param event Sự kiện phím từ ô input cha.
   * @returns true nếu phím được popover xử lý và chặn mặc định.
   */
  handleKeyDown(event: KeyboardEvent): boolean {
    if (!this.visible()) {
      return false;
    }

    const members = this.filteredMembers();
    if (members.length === 0) {
      if (event.key === 'Escape') {
        this.closePopover.emit();
        return true;
      }
      return false;
    }

    switch (event.key) {
      case 'ArrowDown': {
        event.preventDefault();
        const next = (this.selectedIndex() + 1) % members.length;
        this.selectedIndex.set(next);
        return true;
      }

      case 'ArrowUp': {
        event.preventDefault();
        const prev = (this.selectedIndex() - 1 + members.length) % members.length;
        this.selectedIndex.set(prev);
        return true;
      }

      case 'Enter':
      case 'Tab': {
        event.preventDefault();
        const current = members[this.selectedIndex()];
        if (current) {
          this.onSelect(current);
        }
        return true;
      }

      case 'Escape': {
        event.preventDefault();
        this.closePopover.emit();
        return true;
      }

      default:
        return false;
    }
  }

  /**
   * Lựa chọn một thành viên cụ thể.
   *
   * @param member Thành viên được chọn.
   */
  onSelect(member: Member): void {
    this.selectMember.emit(member);
  }
}
