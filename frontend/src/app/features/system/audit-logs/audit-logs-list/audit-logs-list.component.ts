/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCardComponent } from '../../../../shared/components/card/card.component';
import { AppIconComponent } from '../../../../shared/components/icon/icon.component';
import { AppBadgeComponent } from '../../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-audit-logs-list',
  standalone: true,
  imports: [CommonModule, AppCardComponent, AppIconComponent, AppBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-6 sm:py-8 space-y-6">
      <!-- Page Header -->
      <div>
        <h2 class="text-xl sm:text-2xl font-bold text-black dark:text-white tracking-tight">
          Nhật ký Kiểm toán Hệ thống (Audit Logs)
        </h2>
        <p class="text-xs sm:text-sm text-body dark:text-bodydark mt-1">
          Hạ tầng ghi vết an ninh, giám sát các thay đổi thực thể và tự động dọn dẹp qua Hangfire trên MongoDB
        </p>
      </div>

      <!-- Information Banner Card -->
      <app-card>
        <div class="flex items-start gap-3.5">
          <div class="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-info/10 text-info dark:bg-info/20">
            <app-icon name="audit" [size]="20" />
          </div>
          <div class="space-y-1.5">
            <div class="flex items-center gap-2">
              <h4 class="font-bold text-sm text-black dark:text-white">
                Cơ chế Lưu trữ & Vận hành Tự động
              </h4>
              <app-badge variant="success" size="sm">Đang bảo vệ</app-badge>
            </div>
            <p class="text-xs text-body dark:text-bodydark leading-relaxed">
              Mọi thay đổi trên các thực thể nghiệp vụ (Thêm, Sửa, Xóa mềm, Đổi mật khẩu) đều được Interceptor của EF Core tự động bắt và đẩy vào cơ sở dữ liệu MongoDB Audit Log.
            </p>
            <p class="text-xs text-body dark:text-bodydark leading-relaxed">
              Dữ liệu kiểm toán được bảo quản và tự động loại bỏ sau 90 ngày thông qua tác vụ định kỳ <code class="rounded bg-gray-2 px-1.5 py-0.5 text-black font-mono text-[11px] dark:bg-meta-4 dark:text-white">SystemAuditLogCleanupJob</code> chạy trên nền tảng Hangfire.
            </p>
          </div>
        </div>
      </app-card>
    </div>
  `,
})
export class AuditLogsListComponent {}
