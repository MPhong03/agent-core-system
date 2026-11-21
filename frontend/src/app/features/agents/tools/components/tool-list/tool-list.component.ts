/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-list.component.ts — Giao diện Quản lý Danh sách Công cụ AI (Sprint 3 - PBI-TOOL-01)
 *
 * ## Vai trò Kiến trúc
 * Trang trung tâm No-Code Tool Builder:
 * - Hiển thị danh mục các REST API Tools đã đăng ký.
 * - Tìm kiếm theo tên / tên hiển thị, lọc theo phương thức HTTP.
 * - Badge màu phương thức HTTP (GET xanh dương, POST xanh lục, PUT vàng, DELETE đỏ, PATCH tím).
 * - Cổng xác nhận người dùng (Yêu cầu duyệt vs Tự động).
 * - Thao tác chạy thử nghiệm (Sandbox Test), tạo mới, chỉnh sửa và xóa công cụ.
 */

import {
  Component,
  ChangeDetectionStrategy,
  inject,
  OnInit,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToolsService } from '../../services/tools.service';
import { ToolDefinitionDto } from '../../models/tool.models';
import { ToolEditorComponent } from '../tool-editor/tool-editor.component';
import { ToolTestDialogComponent } from '../tool-test-dialog/tool-test-dialog.component';
import { AppButtonComponent } from '../../../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../../../shared/components/icon/icon.component';

@Component({
  selector: 'app-tool-list',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ToolEditorComponent,
    ToolTestDialogComponent,
    AppButtonComponent,
    AppIconComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="max-w-7xl mx-auto w-full px-4 sm:px-6 lg:px-8 py-6 sm:py-8 space-y-6">
      <!-- ── Page Header ── -->
      <div class="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <div class="flex items-center gap-2.5">
            <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-primary/10 text-primary dark:bg-primary/20 dark:text-white">
              <app-icon name="code" [size]="22" />
            </div>
            <div>
              <h2 class="text-xl sm:text-2xl font-bold text-black dark:text-white tracking-tight">
                Quản lý Công cụ AI (Tool Builder)
              </h2>
              <p class="text-xs sm:text-sm text-body dark:text-bodydark mt-0.5">
                Đăng ký REST API endpoints cho AI Agents thực hiện Function Calling an toàn
              </p>
            </div>
          </div>
        </div>

        <div>
          <app-button variant="primary" size="md" (click)="openCreateModal()">
            <app-icon name="plus" [size]="18" class="mr-1.5" />
            <span>Thêm công cụ mới</span>
          </app-button>
        </div>
      </div>

      <!-- ── Main Card ── -->
      <div class="rounded-xl border border-stroke bg-white shadow-xs dark:border-strokedark dark:bg-boxdark overflow-hidden">
        <!-- Toolbar: Search & Filter -->
        <div class="p-4 sm:p-5 border-b border-stroke dark:border-strokedark flex flex-col md:flex-row items-center justify-between gap-4">
          <!-- Search input -->
          <div class="relative flex-1 w-full md:max-w-md">
            <input
              type="text"
              [(ngModel)]="searchKeyword"
              placeholder="Tìm kiếm theo tên kỹ thuật hoặc tên hiển thị..."
              class="w-full h-10 rounded-lg border border-stroke bg-transparent pl-10 pr-4 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
            <span class="pointer-events-none absolute inset-y-0 left-0 flex items-center pl-3.5 text-body dark:text-bodydark">
              <app-icon name="search" [size]="18" />
            </span>
          </div>

          <!-- HTTP Method Filter -->
          <div class="flex items-center gap-2 w-full md:w-auto">
            <span class="text-xs font-semibold text-body dark:text-bodydark whitespace-nowrap">
              Phương thức:
            </span>
            <div class="flex flex-wrap gap-1.5">
              @for (method of methodFilters; track method) {
                <button
                  type="button"
                  (click)="selectedMethod.set(method)"
                  class="px-2.5 py-1 text-xs font-semibold rounded-md border transition-colors"
                  [ngClass]="selectedMethod() === method
                    ? 'bg-primary text-white border-primary shadow-xs'
                    : 'bg-transparent text-body hover:bg-gray-2 dark:text-bodydark dark:hover:bg-meta-4 border-stroke dark:border-strokedark'"
                >
                  {{ method }}
                </button>
              }
            </div>
          </div>
        </div>

        <!-- ── Content Table ── -->
        @if (toolsService.isLoading()) {
          <div class="p-12 text-center text-sm text-body dark:text-bodydark flex items-center justify-center gap-2.5">
            <app-icon name="refresh" [size]="20" class="animate-spin text-primary" />
            <span>Đang tải danh sách công cụ AI...</span>
          </div>
        } @else if (filteredTools().length === 0) {
          <div class="p-12 text-center space-y-3">
            <div class="mx-auto flex h-14 w-14 items-center justify-center rounded-full bg-gray-2 dark:bg-meta-4 text-body dark:text-bodydark">
              <app-icon name="code" [size]="28" />
            </div>
            <div class="font-medium text-sm text-black dark:text-white">
              Không tìm thấy công cụ nào phù hợp
            </div>
            <p class="text-xs text-body dark:text-bodydark max-w-sm mx-auto">
              Chưa có công cụ nào khớp với bộ lọc tìm kiếm. Bạn có thể xóa từ khóa lọc hoặc đăng ký công cụ REST API mới.
            </p>
            <div>
              <app-button variant="outline" size="sm" (click)="openCreateModal()">
                <app-icon name="plus" [size]="16" class="mr-1" />
                <span>Tạo công cụ đầu tiên</span>
              </app-button>
            </div>
          </div>
        } @else {
          <div class="max-w-full overflow-x-auto">
            <table class="w-full table-auto">
              <thead>
                <tr class="bg-gray-2 text-left dark:bg-meta-4">
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    CÔNG CỤ & MÔ TẢ
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    PHƯƠNG THỨC
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    ENDPOINT URL
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    PHÊ DUYỆT
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider">
                    NGÀY TẠO
                  </th>
                  <th class="py-3.5 px-6 font-semibold text-xs text-black dark:text-white uppercase tracking-wider text-right">
                    THAO TÁC
                  </th>
                </tr>
              </thead>
              <tbody class="divide-y divide-stroke dark:divide-strokedark">
                @for (tool of filteredTools(); track tool.id) {
                  <tr class="hover:bg-gray-2/50 dark:hover:bg-meta-4/20 transition-colors">
                    <!-- Tool Info -->
                    <td class="py-4 px-6">
                      <div class="space-y-0.5">
                        <div class="font-semibold text-sm text-black dark:text-white flex items-center gap-2">
                          <span>{{ tool.displayName }}</span>
                          <span class="text-xs font-mono font-normal text-body dark:text-bodydark">
                            ({{ tool.name }})
                          </span>
                        </div>
                        <p class="text-xs text-body dark:text-bodydark line-clamp-1 max-w-md">
                          {{ tool.description }}
                        </p>
                      </div>
                    </td>

                    <!-- Method Badge -->
                    <td class="py-4 px-6">
                      <span
                        class="px-2.5 py-1 rounded-md text-xs font-bold font-mono uppercase border"
                        [ngClass]="methodBadgeClass(tool.httpMethod)"
                      >
                        {{ tool.httpMethod }}
                      </span>
                    </td>

                    <!-- Endpoint URL -->
                    <td class="py-4 px-6">
                      <div class="font-mono text-xs text-black dark:text-white truncate max-w-xs" [title]="tool.endpointUrl">
                        {{ tool.endpointUrl }}
                      </div>
                    </td>

                    <!-- Requires Confirmation Gate -->
                    <td class="py-4 px-6">
                      @if (tool.requiresConfirmation) {
                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-amber-50 text-amber-700 border border-amber-200 dark:bg-amber-900/30 dark:text-amber-300 dark:border-amber-800">
                          <app-icon name="shield" [size]="12" />
                          <span>Yêu cầu duyệt</span>
                        </span>
                      } @else {
                        <span class="inline-flex items-center gap-1 px-2 py-0.5 rounded text-xs font-medium bg-gray-2 text-body border border-stroke dark:bg-meta-4 dark:text-bodydark dark:border-strokedark">
                          <span>Tự động</span>
                        </span>
                      }
                    </td>

                    <!-- Created Date -->
                    <td class="py-4 px-6 text-xs text-body dark:text-bodydark whitespace-nowrap">
                      {{ tool.createdDate | date: 'dd/MM/yyyy HH:mm' }}
                    </td>

                    <!-- Action Buttons -->
                    <td class="py-4 px-6 text-right whitespace-nowrap">
                      <div class="flex items-center justify-end gap-1.5">
                        <!-- Test Button -->
                        <button
                          type="button"
                          (click)="openTestModal(tool)"
                          class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium rounded-md bg-primary/10 text-primary hover:bg-primary/20 dark:bg-primary/20 dark:text-white transition-colors"
                          title="Chạy thử nghiệm công cụ này"
                        >
                          <app-icon name="send" [size]="13" />
                          <span>Chạy thử</span>
                        </button>

                        <!-- Edit Button -->
                        <button
                          type="button"
                          (click)="openEditModal(tool)"
                          class="p-1.5 text-body hover:text-black dark:text-bodydark dark:hover:text-white rounded-md hover:bg-gray-2 dark:hover:bg-meta-4 transition-colors"
                          title="Chỉnh sửa công cụ"
                        >
                          <app-icon name="edit" [size]="16" />
                        </button>

                        <!-- Delete Button -->
                        <button
                          type="button"
                          (click)="confirmDelete(tool)"
                          class="p-1.5 text-danger hover:text-danger/80 rounded-md hover:bg-danger/10 transition-colors"
                          title="Xóa công cụ này"
                        >
                          <app-icon name="trash" [size]="16" />
                        </button>
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>

          <!-- Table Footer Info -->
          <div class="p-4 border-t border-stroke dark:border-strokedark text-xs text-body dark:text-bodydark flex items-center justify-between">
            <span>
              Hiển thị {{ filteredTools().length }} / {{ toolsService.tools().length }} công cụ
            </span>
            <span class="font-mono">
              Bảo vệ an toàn SSRF đa tầng & Khóa xác nhận người dùng
            </span>
          </div>
        }
      </div>
    </div>

    <!-- ── Tool Editor Modal ── -->
    @if (isEditorOpen()) {
      <app-tool-editor
        [tool]="selectedToolForEdit()"
        [isOpen]="isEditorOpen()"
        (close)="closeEditorModal()"
        (saved)="onToolSaved($event)"
      />
    }

    <!-- ── Tool Test Dialog Modal ── -->
    @if (isTestOpen()) {
      <app-tool-test-dialog
        [tool]="selectedToolForTest()"
        [isOpen]="isTestOpen()"
        (close)="closeTestModal()"
      />
    }
  `,
})
export class ToolListComponent implements OnInit {
  readonly toolsService = inject(ToolsService);

  readonly searchKeyword = signal<string>('');
  readonly selectedMethod = signal<string>('ALL');

  readonly methodFilters: string[] = ['ALL', 'GET', 'POST', 'PUT', 'DELETE', 'PATCH'];

  // Modal states
  readonly isEditorOpen = signal<boolean>(false);
  readonly selectedToolForEdit = signal<ToolDefinitionDto | null>(null);

  readonly isTestOpen = signal<boolean>(false);
  readonly selectedToolForTest = signal<ToolDefinitionDto | null>(null);

  readonly filteredTools = computed(() => {
    const keyword = this.searchKeyword().trim().toLowerCase();
    const method = this.selectedMethod();
    const all = this.toolsService.tools();

    return all.filter((tool) => {
      const matchKeyword =
        !keyword ||
        tool.name.toLowerCase().includes(keyword) ||
        tool.displayName.toLowerCase().includes(keyword) ||
        (tool.description && tool.description.toLowerCase().includes(keyword));

      const matchMethod = method === 'ALL' || tool.httpMethod.toUpperCase() === method;

      return matchKeyword && matchMethod;
    });
  });

  ngOnInit(): void {
    this.toolsService.loadTools().subscribe();
  }

  openCreateModal(): void {
    this.selectedToolForEdit.set(null);
    this.isEditorOpen.set(true);
  }

  openEditModal(tool: ToolDefinitionDto): void {
    this.selectedToolForEdit.set(tool);
    this.isEditorOpen.set(true);
  }

  closeEditorModal(): void {
    this.isEditorOpen.set(false);
    this.selectedToolForEdit.set(null);
  }

  onToolSaved(tool: ToolDefinitionDto): void {
    // List is already updated in signal by service
  }

  openTestModal(tool: ToolDefinitionDto): void {
    this.selectedToolForTest.set(tool);
    this.isTestOpen.set(true);
  }

  closeTestModal(): void {
    this.isTestOpen.set(false);
    this.selectedToolForTest.set(null);
  }

  confirmDelete(tool: ToolDefinitionDto): void {
    const confirmed = confirm(
      `Bạn có chắc chắn muốn xóa công cụ "${tool.displayName}" (${tool.name}) không?`,
    );
    if (confirmed) {
      this.toolsService.deleteTool(tool.id).subscribe();
    }
  }

  methodBadgeClass(method: string): string {
    const upper = method.toUpperCase();
    switch (upper) {
      case 'GET':
        return 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-900/30 dark:text-blue-300 dark:border-blue-800';
      case 'POST':
        return 'bg-emerald-50 text-emerald-700 border-emerald-200 dark:bg-emerald-900/30 dark:text-emerald-300 dark:border-emerald-800';
      case 'PUT':
        return 'bg-amber-50 text-amber-700 border-amber-200 dark:bg-amber-900/30 dark:text-amber-300 dark:border-amber-800';
      case 'DELETE':
        return 'bg-rose-50 text-rose-700 border-rose-200 dark:bg-rose-900/30 dark:text-rose-300 dark:border-rose-800';
      case 'PATCH':
        return 'bg-purple-50 text-purple-700 border-purple-200 dark:bg-purple-900/30 dark:text-purple-300 dark:border-purple-800';
      default:
        return 'bg-gray-100 text-gray-700 border-gray-300 dark:bg-gray-800 dark:text-gray-300 dark:border-gray-700';
    }
  }
}
