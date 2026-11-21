/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-execution-badge.component.ts — Huy hiệu báo hiệu đang thực thi công cụ (Sprint 3 - Task 10)
 *
 * ## Trải Nghiệm Người Dùng Realtime
 * Hiển thị huy hiệu hoạt ảnh sinh động (animated badge) thông báo cho người dùng biết AI Agent
 * đang trực tiếp thực thi một công cụ cụ thể (ví dụ: `execute_sql_query`, `weather_lookup`).
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05)
 * 🔴 Standalone component với OnPush change detection strategy.
 * 🔴 Tên công cụ hiển thị font monospace và nhãn mô tả tiếng Việt rõ ràng.
 */

import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-tool-execution-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './tool-execution-badge.component.html',
  styleUrls: ['./tool-execution-badge.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToolExecutionBadgeComponent {
  // ── Inputs (Angular 19 Signal Input) ──

  /**
   * Tên của công cụ đang được thực thi.
   */
  readonly toolName = input<string>('');
}
