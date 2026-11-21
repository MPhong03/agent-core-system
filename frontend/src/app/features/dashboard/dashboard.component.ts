/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Component, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { AppCardComponent } from '../../shared/components/card/card.component';
import { AppIconComponent } from '../../shared/components/icon/icon.component';
import { AppButtonComponent } from '../../shared/components/button/button.component';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    TranslatePipe,
    AppCardComponent,
    AppIconComponent,
    AppButtonComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-6 sm:py-8 space-y-6">
      <!-- Welcome Hero Banner -->
      <div class="relative overflow-hidden rounded-xl border border-stroke bg-white p-6 sm:p-8 shadow-xs dark:border-strokedark dark:bg-boxdark">
        <div class="max-w-2xl">
          <div class="inline-flex items-center gap-2 rounded-full bg-primary/10 px-3 py-1 text-xs font-semibold text-primary dark:bg-primary/20 dark:text-secondary mb-3">
            <app-icon name="shield" [size]="14" />
            <span>AgentCore System v1.0</span>
          </div>
          <h2 class="text-xl sm:text-2xl font-bold text-black dark:text-white tracking-tight">
            {{ 'DASHBOARD.WELCOME_TITLE' | translate }}
          </h2>
          <p class="mt-2 text-sm text-body dark:text-bodydark leading-relaxed">
            {{ 'DASHBOARD.WELCOME_SUB' | translate }}
          </p>
        </div>
      </div>

      <!-- 4 Architectural Highlights / Stat Cards -->
      <div class="grid grid-cols-1 gap-5 sm:grid-cols-2 xl:grid-cols-4">
        <!-- Backend Card -->
        <div class="rounded-xl border border-stroke bg-white p-5 shadow-xs dark:border-strokedark dark:bg-boxdark flex items-center gap-4 transition-all hover:shadow-card">
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary dark:bg-primary/20 dark:text-secondary">
            <app-icon name="database" [size]="24" />
          </div>
          <div class="min-w-0">
            <span class="text-xs font-semibold text-body dark:text-bodydark uppercase tracking-wider block">
              {{ 'DASHBOARD.CARD_BACKEND' | translate }}
            </span>
            <h4 class="text-base font-bold text-black dark:text-white truncate mt-0.5">
              Modular Monolith
            </h4>
            <span class="text-xs text-body dark:text-bodydark truncate block mt-0.5 font-mono">
              Schema [system] SQL Server
            </span>
          </div>
        </div>

        <!-- Jobs Card -->
        <div class="rounded-xl border border-stroke bg-white p-5 shadow-xs dark:border-strokedark dark:bg-boxdark flex items-center gap-4 transition-all hover:shadow-card">
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-success/10 text-success dark:bg-success/20 dark:text-meta-3">
            <app-icon name="server" [size]="24" />
          </div>
          <div class="min-w-0">
            <span class="text-xs font-semibold text-body dark:text-bodydark uppercase tracking-wider block">
              {{ 'DASHBOARD.CARD_JOBS' | translate }}
            </span>
            <h4 class="text-base font-bold text-black dark:text-white truncate mt-0.5">
              Hangfire + MongoDB
            </h4>
            <span class="text-xs text-body dark:text-bodydark truncate block mt-0.5 font-mono">
              Fail-fast connections
            </span>
          </div>
        </div>

        <!-- API Card -->
        <div class="rounded-xl border border-stroke bg-white p-5 shadow-xs dark:border-strokedark dark:bg-boxdark flex items-center gap-4 transition-all hover:shadow-card">
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-info/10 text-info dark:bg-info/20 dark:text-meta-5">
            <app-icon name="code" [size]="24" />
          </div>
          <div class="min-w-0">
            <span class="text-xs font-semibold text-body dark:text-bodydark uppercase tracking-wider block">
              {{ 'DASHBOARD.CARD_API' | translate }}
            </span>
            <h4 class="text-base font-bold text-black dark:text-white truncate mt-0.5">
              v1.0 RESTful API
            </h4>
            <span class="text-xs text-body dark:text-bodydark truncate block mt-0.5 font-mono">
              RFC 7807 & i18n
            </span>
          </div>
        </div>

        <!-- Frontend Card -->
        <div class="rounded-xl border border-stroke bg-white p-5 shadow-xs dark:border-strokedark dark:bg-boxdark flex items-center gap-4 transition-all hover:shadow-card">
          <div class="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl bg-warning/10 text-warning dark:bg-warning/20 dark:text-meta-6">
            <app-icon name="shield" [size]="24" />
          </div>
          <div class="min-w-0">
            <span class="text-xs font-semibold text-body dark:text-bodydark uppercase tracking-wider block">
              {{ 'DASHBOARD.CARD_FRONTEND' | translate }}
            </span>
            <h4 class="text-base font-bold text-black dark:text-white truncate mt-0.5">
              Angular Standalone
            </h4>
            <span class="text-xs text-body dark:text-bodydark truncate block mt-0.5 font-mono">
              Signals + OnPush + TailAdmin
            </span>
          </div>
        </div>
      </div>

      <!-- Quick Access Modules Grid -->
      <div class="grid grid-cols-1 gap-6 md:grid-cols-2">
        <!-- Workspace Quick Card -->
        <app-card title="Không Gian Làm Việc & Kênh Nhóm" subtitle="Cây phòng ban, Kanban Board và Real-time Chat">
          <div class="space-y-3">
            <p class="text-xs text-body dark:text-bodydark leading-relaxed">
              Truy cập vào không gian làm việc tập trung để quản lý các tác vụ Kanban, phê duyệt Review Gate, và tương tác thời gian thực với các Agents thông qua SignalR.
            </p>
            <div class="pt-2">
              <a routerLink="/organization">
                <app-button variant="primary" size="sm">
                  <app-icon name="organization" [size]="16" class="mr-2" />
                  Mở Không Gian Làm Việc
                </app-button>
              </a>
            </div>
          </div>
        </app-card>

        <!-- System Administration Quick Card -->
        <app-card title="Quản Trị Người Dùng & Phân Quyền" subtitle="Quản lý tài khoản, vai trò và lịch sử kiểm toán">
          <div class="space-y-3">
            <p class="text-xs text-body dark:text-bodydark leading-relaxed">
              Tra cứu danh sách nhân sự, phân quyền vai trò bảo mật (SuperAdmin, Admin, Employee) và theo dõi toàn bộ nhật ký thay đổi của hệ thống.
            </p>
            <div class="pt-2">
              <a routerLink="/system/users">
                <app-button variant="secondary" size="sm">
                  <app-icon name="users" [size]="16" class="mr-2" />
                  Quản Trị Người Dùng
                </app-button>
              </a>
            </div>
          </div>
        </app-card>
      </div>
    </div>
  `,
})
export class DashboardComponent {}
