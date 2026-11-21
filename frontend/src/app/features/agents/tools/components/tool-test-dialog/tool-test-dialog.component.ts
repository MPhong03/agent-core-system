/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-test-dialog.component.ts — Hộp thoại Kiểm thử Thực thi Công cụ AI (Sprint 3 - PBI-TOOL-01)
 *
 * ## Vai trò Kiến trúc
 * Cung cấp sandbox kiểm thử trực tiếp REST API Endpoint:
 * - Hiển thị thông số HTTP Method, Endpoint URL và JSON Schema.
 * - Cho phép nhập Arguments dạng JSON với kiểm tra cú pháp thời gian thực.
 * - Gọi backend kiểm thử an toàn qua SSRF Safe Web Client (`/api/v1/agents/tools/{id}/test`).
 * - Hiển thị phản hồi chi tiết: HTTP Status Code, Thời gian thực thi (Latency ms) và Response Body Viewer.
 */

import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  input,
  output,
  OnInit,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToolsService } from '../../services/tools.service';
import {
  ToolDefinitionDto,
  TestToolRequest,
  ToolTestResultDto,
} from '../../models/tool.models';
import { AppModalComponent } from '../../../../../shared/components/modal/modal.component';
import { AppButtonComponent } from '../../../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../../../shared/components/icon/icon.component';
import { AppBadgeComponent } from '../../../../../shared/components/badge/badge.component';

@Component({
  selector: 'app-tool-test-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    AppModalComponent,
    AppButtonComponent,
    AppIconComponent,
    AppBadgeComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-modal
      [isOpen]="isOpen()"
      [title]="modalTitle()"
      size="xl"
      (close)="onClose()"
    >
      <div class="space-y-5">
        <!-- ── Tool Info Banner ── -->
        @if (tool(); as currentTool) {
          <div class="rounded-lg border border-stroke dark:border-strokedark p-4 bg-gray-2/50 dark:bg-meta-4/20 space-y-2">
            <div class="flex flex-wrap items-center justify-between gap-2">
              <div class="flex items-center gap-2">
                <span
                  class="px-2.5 py-0.5 rounded text-xs font-bold font-mono uppercase"
                  [ngClass]="methodBadgeClass(currentTool.httpMethod)"
                >
                  {{ currentTool.httpMethod }}
                </span>
                <span class="font-semibold text-sm text-black dark:text-white">
                  {{ currentTool.displayName }}
                </span>
                <span class="text-xs text-body dark:text-bodydark font-mono">
                  ({{ currentTool.name }})
                </span>
              </div>

              @if (currentTool.requiresConfirmation) {
                <app-badge variant="warning" size="sm">
                  Yêu cầu duyệt
                </app-badge>
              }
            </div>

            <div class="text-xs font-mono text-body dark:text-bodydark break-all">
              <span class="font-semibold text-black dark:text-white">URL:</span>
              {{ currentTool.endpointUrl }}
            </div>

            @if (currentTool.description) {
              <div class="text-xs text-body dark:text-bodydark italic">
                {{ currentTool.description }}
              </div>
            }
          </div>
        }

        <!-- ── Dynamic Arguments Input ── -->
        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <label class="text-xs font-semibold text-black dark:text-white flex items-center gap-2">
              <span>Tham số Giả lập Thực thi (Arguments JSON)</span>
              @if (argsError()) {
                <span class="inline-flex items-center gap-1 text-[11px] text-danger font-normal">
                  <app-icon name="close" [size]="12" />
                  Cú pháp JSON lỗi
                </span>
              } @else {
                <span class="inline-flex items-center gap-1 text-[11px] text-success font-normal">
                  <app-icon name="check" [size]="12" />
                  JSON hợp lệ
                </span>
              }
            </label>

            <button
              type="button"
              (click)="formatArgsJson()"
              class="inline-flex items-center gap-1 text-xs text-primary hover:underline"
            >
              <app-icon name="code" [size]="14" />
              <span>Định dạng JSON</span>
            </button>
          </div>

          <textarea
            [(ngModel)]="argumentsJson"
            (ngModelChange)="onArgsChange()"
            rows="5"
            placeholder='{ "key": "value" }'
            class="w-full rounded-lg border font-mono text-xs p-3 outline-none transition-colors focus:border-primary dark:bg-boxdark dark:text-white custom-scrollbar"
            [class.border-danger]="argsError()"
            [class.border-stroke]="!argsError()"
            [class.dark:border-strokedark]="!argsError()"
          ></textarea>

          @if (argsError()) {
            <p class="text-[11px] text-danger font-mono">
              {{ argsError() }}
            </p>
          }
        </div>

        <!-- ── Execution Button ── -->
        <div class="flex items-center justify-between pt-1">
          <span class="text-xs text-body dark:text-bodydark">
            Kiểm thử chạy qua proxy bảo vệ SSRF nội bộ.
          </span>

          <app-button
            variant="primary"
            size="md"
            [disabled]="isExecuting() || !!argsError()"
            (click)="executeTest()"
          >
            @if (isExecuting()) {
              <app-icon name="refresh" [size]="16" class="animate-spin mr-1.5" />
              <span>Đang gửi yêu cầu...</span>
            } @else {
              <app-icon name="send" [size]="16" class="mr-1.5" />
              <span>Gửi yêu cầu thử nghiệm</span>
            }
          </app-button>
        </div>

        <!-- ── Execution Result Viewer ── -->
        @if (testResult(); as res) {
          <div class="rounded-lg border border-stroke dark:border-strokedark p-4 bg-gray-2/30 dark:bg-meta-4/10 space-y-3">
            <div class="flex flex-wrap items-center justify-between gap-2 border-b border-stroke dark:border-strokedark pb-3">
              <div class="flex items-center gap-2">
                <span class="text-xs font-semibold text-black dark:text-white">
                  Kết quả Phản hồi:
                </span>
                <span
                  class="px-2 py-0.5 rounded text-xs font-bold font-mono"
                  [ngClass]="res.statusCode >= 200 && res.statusCode < 300
                    ? 'bg-success/10 text-success border border-success/30'
                    : 'bg-danger/10 text-danger border border-danger/30'"
                >
                  HTTP {{ res.statusCode }}
                </span>
                <span class="text-xs font-medium text-body dark:text-bodydark font-mono">
                  ⏱ {{ res.durationMs }}ms
                </span>
              </div>

              <div class="flex items-center gap-2">
                <button
                  type="button"
                  (click)="copyPayload()"
                  class="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-body hover:text-black dark:text-bodydark dark:hover:text-white rounded border border-stroke dark:border-strokedark transition-colors"
                >
                  <app-icon name="code" [size]="13" />
                  <span>{{ isCopied() ? 'Đã sao chép!' : 'Sao chép JSON' }}</span>
                </button>
              </div>
            </div>

            @if (res.errorMessage) {
              <div class="p-3 rounded-md bg-danger/10 border border-danger/20 text-xs text-danger font-mono">
                <strong>Lỗi:</strong> {{ res.errorMessage }}
              </div>
            }

            <div>
              <div class="text-[11px] font-semibold text-body dark:text-bodydark uppercase tracking-wider mb-1.5">
                Response Payload:
              </div>
              <pre class="w-full max-h-60 overflow-auto rounded-md bg-black/90 p-3 text-xs font-mono text-emerald-400 custom-scrollbar">{{ formattedResponsePayload() }}</pre>
            </div>
          </div>
        }
      </div>

      <!-- Modal Footer -->
      <div modal-footer class="flex items-center justify-end w-full">
        <app-button
          variant="secondary"
          size="md"
          (click)="onClose()"
        >
          Đóng
        </app-button>
      </div>
    </app-modal>
  `,
})
export class ToolTestDialogComponent implements OnInit {
  private readonly toolsService = inject(ToolsService);

  readonly tool = input<ToolDefinitionDto | null>(null);
  readonly isOpen = input<boolean>(true);
  readonly close = output<void>();

  readonly modalTitle = computed(() => {
    const t = this.tool();
    return t ? `Chạy thử Công cụ: ${t.displayName}` : 'Chạy thử Công cụ AI';
  });

  argumentsJson = '{}';
  readonly argsError = signal<string | null>(null);
  readonly isExecuting = signal<boolean>(false);
  readonly testResult = signal<ToolTestResultDto | null>(null);
  readonly isCopied = signal<boolean>(false);

  readonly formattedResponsePayload = computed(() => {
    const payload = this.testResult()?.responsePayload;
    if (!payload) return '(Không có nội dung phản hồi)';
    try {
      const parsed = JSON.parse(payload);
      return JSON.stringify(parsed, null, 2);
    } catch {
      return payload;
    }
  });

  ngOnInit(): void {
    const current = this.tool();
    if (current?.parametersSchema) {
      try {
        const schema = JSON.parse(current.parametersSchema);
        if (schema?.properties) {
          const sampleArgs: Record<string, any> = {};
          Object.keys(schema.properties).forEach((propKey) => {
            const prop = schema.properties[propKey];
            if (prop.type === 'string') {
              sampleArgs[propKey] = prop.default || 'sample_value';
            } else if (prop.type === 'number' || prop.type === 'integer') {
              sampleArgs[propKey] = prop.default || 0;
            } else if (prop.type === 'boolean') {
              sampleArgs[propKey] = prop.default || false;
            } else {
              sampleArgs[propKey] = null;
            }
          });
          this.argumentsJson = JSON.stringify(sampleArgs, null, 2);
        }
      } catch {
        this.argumentsJson = '{}';
      }
    }
    this.validateArgsJson();
  }

  onArgsChange(): void {
    this.validateArgsJson();
  }

  validateArgsJson(): void {
    const raw = this.argumentsJson.trim();
    if (!raw) {
      this.argsError.set(null);
      return;
    }
    try {
      JSON.parse(raw);
      this.argsError.set(null);
    } catch (e: any) {
      this.argsError.set(e.message || 'Lỗi cú pháp JSON');
    }
  }

  formatArgsJson(): void {
    const raw = this.argumentsJson.trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      this.argumentsJson = JSON.stringify(parsed, null, 2);
      this.argsError.set(null);
    } catch (e: any) {
      this.argsError.set(e.message || 'Lỗi cú pháp JSON');
    }
  }

  executeTest(): void {
    const currentTool = this.tool();
    if (!currentTool) return;

    this.validateArgsJson();
    if (this.argsError()) return;

    this.isExecuting.set(true);
    this.testResult.set(null);

    const testReq: TestToolRequest = {
      argumentsJson: this.argumentsJson.trim() || '{}',
      timeoutSeconds: currentTool.timeoutSeconds || 30,
    };

    this.toolsService.testSavedTool(currentTool.id, testReq).subscribe({
      next: (res) => {
        this.isExecuting.set(false);
        if (res.success && res.data) {
          this.testResult.set(res.data);
        }
      },
      error: (err) => {
        this.isExecuting.set(false);
        this.testResult.set({
          isSuccess: false,
          statusCode: err?.status || 500,
          responsePayload: err?.detail || err?.message || 'Yêu cầu kiểm thử thất bại.',
          durationMs: 0,
          errorMessage: err?.title || 'Lỗi mạng hoặc không thể kết nối tới server.',
        });
      },
    });
  }

  copyPayload(): void {
    const payload = this.formattedResponsePayload();
    if (navigator?.clipboard) {
      navigator.clipboard.writeText(payload).then(() => {
        this.isCopied.set(true);
        setTimeout(() => this.isCopied.set(false), 2000);
      });
    }
  }

  methodBadgeClass(method: string): string {
    const upper = method.toUpperCase();
    switch (upper) {
      case 'GET':
        return 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300';
      case 'POST':
        return 'bg-emerald-100 text-emerald-700 dark:bg-emerald-900/40 dark:text-emerald-300';
      case 'PUT':
        return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300';
      case 'DELETE':
        return 'bg-rose-100 text-rose-700 dark:bg-rose-900/40 dark:text-rose-300';
      case 'PATCH':
        return 'bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300';
      default:
        return 'bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300';
    }
  }

  onClose(): void {
    this.close.emit();
  }
}
