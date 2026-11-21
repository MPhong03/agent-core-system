/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AppCardComponent } from '../../../../shared/components/card/card.component';
import { AppBadgeComponent } from '../../../../shared/components/badge/badge.component';
import { AppIconComponent } from '../../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-roles-list',
  standalone: true,
  imports: [CommonModule, AppCardComponent, AppBadgeComponent, AppIconComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="space-y-6">
      <!-- Page Header -->
      <div>
        <h2 class="text-xl sm:text-2xl font-bold text-black dark:text-white tracking-tight">
          Quản lý Vai trò & Phân quyền (RBAC)
        </h2>
        <p class="text-xs sm:text-sm text-body dark:text-bodydark mt-1">
          Hệ thống Identity phân quyền dựa trên 3 vai trò mặc định: SuperAdmin, Admin, Employee
        </p>
      </div>

      <!-- Roles Grid -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4 md:gap-6">
        <!-- SuperAdmin Card -->
        <app-card>
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-danger/10 text-danger dark:bg-danger/20">
                <app-icon name="shield" [size]="20" />
              </div>
              <app-badge variant="danger" size="sm">SuperAdmin</app-badge>
            </div>
            <div>
              <h3 class="text-base font-bold text-black dark:text-white">
                Quản trị viên Tối cao
              </h3>
              <p class="text-xs text-body dark:text-bodydark mt-1.5 leading-relaxed">
                Toàn quyền trên toàn bộ hệ thống. Có thể cấu hình cơ sở hạ tầng, quản lý tất cả các phân hệ và bypass kiểm tra quyền hạn.
              </p>
            </div>
            <div class="pt-2 border-t border-stroke dark:border-strokedark flex items-center gap-2 text-[11px] text-body dark:text-bodydark">
              <span class="inline-block h-1.5 w-1.5 rounded-full bg-danger"></span>
              <span>Quyền hạn: Toàn quyền truy cập và kiểm soát</span>
            </div>
          </div>
        </app-card>

        <!-- Admin Card -->
        <app-card>
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary dark:bg-primary/20">
                <app-icon name="roles" [size]="20" />
              </div>
              <app-badge variant="primary" size="sm">Admin</app-badge>
            </div>
            <div>
              <h3 class="text-base font-bold text-black dark:text-white">
                Quản trị viên Phân hệ
              </h3>
              <p class="text-xs text-body dark:text-bodydark mt-1.5 leading-relaxed">
                Quản lý tài khoản người dùng, thiết lập phân hệ và theo dõi hoạt động vận hành của chi nhánh hoặc phòng ban được phân bổ.
              </p>
            </div>
            <div class="pt-2 border-t border-stroke dark:border-strokedark flex items-center gap-2 text-[11px] text-body dark:text-bodydark">
              <span class="inline-block h-1.5 w-1.5 rounded-full bg-primary"></span>
              <span>Quyền hạn: Quản lý Users, Tasks, Channels</span>
            </div>
          </div>
        </app-card>

        <!-- Employee Card -->
        <app-card>
          <div class="space-y-3">
            <div class="flex items-center justify-between">
              <div class="flex h-10 w-10 items-center justify-center rounded-lg bg-gray-2 text-body dark:bg-meta-4 dark:text-bodydark">
                <app-icon name="user" [size]="20" />
              </div>
              <app-badge variant="neutral" size="sm">Employee</app-badge>
            </div>
            <div>
              <h3 class="text-base font-bold text-black dark:text-white">
                Nhân viên Tác nghiệp
              </h3>
              <p class="text-xs text-body dark:text-bodydark mt-1.5 leading-relaxed">
                Nhân viên thực hiện các nghiệp vụ thông thường trong phạm vi chức năng được cấp phép, kéo thả công việc Kanban và trò chuyện kênh.
              </p>
            </div>
            <div class="pt-2 border-t border-stroke dark:border-strokedark flex items-center gap-2 text-[11px] text-body dark:text-bodydark">
              <span class="inline-block h-1.5 w-1.5 rounded-full bg-bodydark2"></span>
              <span>Quyền hạn: Tác nghiệp Workspace cơ bản</span>
            </div>
          </div>
        </app-card>
      </div>
    </div>
  `,
})
export class RolesListComponent {}
