/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-audit-logs-list',
  standalone: true,
  imports: [CommonModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="audit-page">
      <div class="page-header">
        <h2>Nhật ký Kiểm toán Hệ thống (Audit Logs)</h2>
        <p>
          Tự động ghi vết các thao tác quan trọng và dọn dẹp định kỳ qua Hangfire Job trên MongoDB
        </p>
      </div>

      <div class="card">
        <div class="info-banner">
          <span
            >ℹ️ Dữ liệu kiểm toán được bảo quản và tự động loại bỏ sau 90 ngày thông qua
            <code>SystemAuditLogCleanupJob</code>.</span
          >
        </div>
      </div>
    </div>
  `,
  styles: [
    `
      .audit-page {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }
      .page-header h2 {
        margin: 0 0 4px 0;
        color: #0f172a;
        font-size: 1.4rem;
      }
      .page-header p {
        margin: 0;
        color: #64748b;
        font-size: 0.85rem;
      }
      .card {
        background: #fff;
        padding: 20px;
        border-radius: 8px;
        border: 1px solid #e2e8f0;
      }
      .info-banner {
        background-color: #f0fdf4;
        border: 1px solid #bbf7d0;
        color: #166534;
        padding: 14px;
        border-radius: 6px;
        font-size: 0.9rem;
      }
      code {
        background: #dcfce7;
        padding: 2px 6px;
        border-radius: 4px;
        font-weight: 600;
      }
    `,
  ],
})
export class AuditLogsListComponent {}
