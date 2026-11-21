/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-approval-card.component.ts — Action Card tương tác phê duyệt thực thi công cụ (Sprint 3 - Task 10)
 *
 * ## Vai Trò & Trải Nghiệm Người Dùng (Human-in-the-Loop)
 * Khi AI Agent cần gọi một công cụ có tính rủi ro hoặc yêu cầu xác nhận của người dùng (Confirmatory Tool),
 * Action Card này sẽ hiển thị trực quan thông tin công cụ, tham số JSON định dạng đẹp mắt,
 * và cung cấp 2 nút hành động [Phê duyệt & Thực thi] hoặc [Từ chối].
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05, RULE-06)
 * 🔴 Khớp Contract: `pendingToolCallId` là UUIDv7 truyền lên API confirm/reject.
 * 🔴 Tối ưu hiển thị: JSON arguments được parse và format thụt dòng 2 space với pre block chống tràn dòng.
 * ⛔ Không cho phép click đúp hoặc click khi đang xử lý (vô hiệu hóa nút và hiển thị spinner).
 * ⇒ Khi trạng thái chuyển thành `confirmed` hoặc `rejected` ⇒ Ẩn các nút thao tác và hiển thị badge trạng thái tương ứng.
 */

import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToolApprovalRequest } from '../../models/chat.models';

@Component({
  selector: 'app-tool-approval-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tool-approval-card.component.html',
  styleUrls: ['./tool-approval-card.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolApprovalCardComponent {
  // ── Inputs (Angular 19 Signal Inputs) ──

  /**
   * Yêu cầu phê duyệt công cụ nhận từ SignalR Hub.
   */
  readonly approvalRequest = input.required<ToolApprovalRequest>();

  /**
   * Cờ xác định yêu cầu phê duyệt/từ chối đang được gửi lên server từ bên ngoài.
   */
  readonly isSubmitting = input<boolean>(false);

  // ── Outputs (Angular 19 Output Emitted Refs) ──

  /**
   * Phát sự kiện khi người dùng bấm phê duyệt, truyền pendingToolCallId.
   */
  readonly confirmed = output<string>();

  /**
   * Phát sự kiện khi người dùng bấm từ chối, truyền callId và lý do tùy chọn.
   */
  readonly rejected = output<{ callId: string; reason?: string }>();

  // ── Local State Signals ──
  readonly isExpanded = signal<boolean>(true);
  readonly localStatus = signal<'pending' | 'confirmed' | 'rejected' | null>(null);

  // ── Computed Signals ──

  /**
   * Trạng thái hiệu lực hiện tại của yêu cầu (pending | confirmed | rejected).
   */
  readonly effectiveStatus = computed<'pending' | 'confirmed' | 'rejected'>(() => {
    return this.localStatus() ?? this.approvalRequest()?.status ?? 'pending';
  });

  /**
   * Chuỗi JSON tham số đã được định dạng (indent 2 spaces) để hiển thị trong khối mã pre.
   */
  readonly formattedArguments = computed<string>(() => {
    const raw = this.approvalRequest()?.argumentsJson;
    if (!raw) {
      return '{}';
    }

    try {
      const parsed = typeof raw === 'string' ? JSON.parse(raw) : raw;
      return JSON.stringify(parsed, null, 2);
    } catch {
      return typeof raw === 'string' ? raw : String(raw);
    }
  });

  // ── User Actions ──

  /**
   * Đóng/mở khung hiển thị chi tiết tham số JSON.
   */
  toggleExpanded(): void {
    this.isExpanded.update((v) => !v);
  }

  /**
   * Xử lý khi người dùng nhấn [Phê duyệt & Thực thi].
   */
  onConfirm(): void {
    if (this.isSubmitting() || this.effectiveStatus() !== 'pending') {
      return;
    }

    const req = this.approvalRequest();
    this.confirmed.emit(req.pendingToolCallId);
  }

  /**
   * Xử lý khi người dùng nhấn [Từ chối].
   */
  onReject(): void {
    if (this.isSubmitting() || this.effectiveStatus() !== 'pending') {
      return;
    }

    const req = this.approvalRequest();
    this.rejected.emit({ callId: req.pendingToolCallId });
  }

  /**
   * Cập nhật trạng thái sau khi API xử lý thành công.
   */
  markCompleted(status: 'confirmed' | 'rejected'): void {
    this.localStatus.set(status);
  }

  /**
   * Reset trạng thái submit khi có lỗi từ server.
   */
  resetSubmitting(): void {
    // Được quản lý trực tiếp qua input isSubmitting từ parent
  }
}
