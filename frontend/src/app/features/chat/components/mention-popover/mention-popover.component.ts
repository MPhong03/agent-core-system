/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
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

/**
 * Popover gợi ý và tự động hoàn thành danh sách thành viên (@mention) trong kênh chat.
 *
 * <remarks>
 * <para><strong>What:</strong> Hiển thị danh sách gợi ý thành viên (Người dùng & AI Agent) khi gõ ký tự '@', hỗ trợ lọc tên và điều hướng bàn phím.</para>
 * <para><strong>When:</strong> Được kích hoạt trong ChatRoomComponent khi con trỏ trong ô nhập tin nhắn đứng sau ký tự '@'.</para>
 * <para><strong>Why:</strong> Cung cấp trải nghiệm gõ @mention mượt mà, phân biệt rõ người thật và trợ lý AI để điều hướng tin nhắn chính xác.</para>
 * </remarks>
 */
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

  /**
   * Chỉ số thành viên đang được chọn (active highlight) bằng bàn phím.
   */
  readonly selectedIndex = signal<number>(0);

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

  constructor() {
    // Tự động reset con trỏ chọn về vị trí đầu tiên khi danh sách lọc thay đổi
    effect(() => {
      this.filteredMembers();
      this.selectedIndex.set(0);
    });
  }

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
