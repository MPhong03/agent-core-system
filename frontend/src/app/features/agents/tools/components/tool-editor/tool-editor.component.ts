/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file tool-editor.component.ts — Form tạo mới và chỉnh sửa Công cụ AI (Sprint 3 - PBI-TOOL-01)
 *
 * ## Vai trò Kiến trúc
 * Cung cấp giao diện trực quan cho người dùng cấu hình:
 * - Tên định danh kỹ thuật, tên hiển thị và mô tả hướng dẫn LLM.
 * - HTTP Method, Endpoint URL bảo vệ SSRF.
 * - Dynamic HTTP Headers key-value editor có che giấu secrets.
 * - JSON Schema Editor cho các tham số hàm gọi (Function Calling parameters).
 * - Cờ yêu cầu phê duyệt người dùng (Human-in-the-loop / Confirmation Gate).
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
import { FormBuilder, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { ToolsService } from '../../services/tools.service';
import {
  ToolDefinitionDto,
  CreateToolRequest,
  UpdateToolRequest,
  HttpMethod,
  HeaderConfigItem,
} from '../../models/tool.models';
import { AppModalComponent } from '../../../../../shared/components/modal/modal.component';
import { AppButtonComponent } from '../../../../../shared/components/button/button.component';
import { AppIconComponent } from '../../../../../shared/components/icon/icon.component';

const DEFAULT_SCHEMA_TEMPLATE = JSON.stringify(
  {
    type: 'object',
    properties: {
      query: {
        type: 'string',
        description: 'Từ khóa tìm kiếm hoặc tham số truy vấn',
      },
    },
    required: ['query'],
  },
  null,
  2,
);

@Component({
  selector: 'app-tool-editor',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AppModalComponent,
    AppButtonComponent,
    AppIconComponent,
  ],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <app-modal
      [isOpen]="isOpen()"
      [title]="modalTitle()"
      size="xl"
      (close)="onCancel()"
    >
      @if (errorMessage()) {
        <div class="mb-4 flex items-center gap-2 rounded-lg bg-danger/10 border border-danger/20 p-3.5 text-xs text-danger dark:bg-danger/20 dark:border-danger/30">
          <app-icon name="close" [size]="16" class="shrink-0" />
          <span>{{ errorMessage() }}</span>
        </div>
      }

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="space-y-5">
        <!-- ── Nhóm 1: Định danh & Thông tin Chung ── -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
          <!-- Name (Technical Identifier) -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              Tên Kỹ thuật (Function Identifier) <span class="text-danger">*</span>
            </label>
            <input
              type="text"
              formControlName="name"
              placeholder="vd: search_database, get_weather"
              [class.bg-gray-100]="isEditMode()"
              [class.cursor-not-allowed]="isEditMode()"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm font-mono text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
            <p class="mt-1 text-[11px] text-body dark:text-bodydark">
              Chỉ gồm chữ cái, số, gạch dưới hoặc gạch ngang (^[a-zA-Z0-9_-]+$). Không thể đổi sau khi tạo.
            </p>
          </div>

          <!-- Display Name -->
          <div>
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              Tên Hiển thị Thân thiện <span class="text-danger">*</span>
            </label>
            <input
              type="text"
              formControlName="displayName"
              placeholder="vd: Tra cứu cơ sở dữ liệu khách hàng"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
          </div>
        </div>

        <!-- Description (Hướng dẫn LLM) -->
        <div>
          <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
            Mô tả Chức năng cho AI Model (Function Prompt) <span class="text-danger">*</span>
          </label>
          <textarea
            formControlName="description"
            rows="2"
            placeholder="Mô tả cụ thể khi nào AI Agent nên gọi công cụ này và công cụ trả về dữ liệu gì..."
            class="w-full rounded-lg border border-stroke bg-transparent p-3 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary custom-scrollbar"
          ></textarea>
        </div>

        <!-- ── Nhóm 2: Cấu hình Endpoint & Phương thức HTTP ── -->
        <div class="grid grid-cols-1 md:grid-cols-4 gap-4">
          <!-- HTTP Method -->
          <div class="md:col-span-1">
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              Phương thức <span class="text-danger">*</span>
            </label>
            <select
              formControlName="httpMethod"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3 text-sm font-semibold text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            >
              <option value="GET">GET</option>
              <option value="POST">POST</option>
              <option value="PUT">PUT</option>
              <option value="DELETE">DELETE</option>
              <option value="PATCH">PATCH</option>
            </select>
          </div>

          <!-- Endpoint URL -->
          <div class="md:col-span-3">
            <label class="mb-1.5 block text-xs font-semibold text-black dark:text-white">
              Địa chỉ URL Endpoint <span class="text-danger">*</span>
            </label>
            <input
              type="text"
              formControlName="endpointUrl"
              placeholder="https://api.external-service.com/v1/resource"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm font-mono text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
          </div>
        </div>

        <!-- ── Nhóm 3: Dynamic Headers Manager ── -->
        <div class="rounded-lg border border-stroke dark:border-strokedark p-4 bg-gray-2/40 dark:bg-meta-4/10 space-y-3">
          <div class="flex items-center justify-between">
            <div>
              <span class="text-xs font-semibold text-black dark:text-white">
                Cấu hình HTTP Headers
              </span>
              <p class="text-[11px] text-body dark:text-bodydark">
                Bảo vệ tự động: Các giá trị nhạy cảm (Authorization, API Keys) được mã hóa AES-GCM tại Backend.
              </p>
            </div>
            <button
              type="button"
              (click)="addHeader()"
              class="inline-flex items-center gap-1.5 px-2.5 py-1 text-xs font-medium text-primary hover:bg-primary/10 rounded-md transition-colors"
            >
              <app-icon name="plus" [size]="14" />
              <span>Thêm Header</span>
            </button>
          </div>

          @if (headers().length === 0) {
            <div class="text-center py-2 text-xs text-body dark:text-bodydark italic">
              Chưa có header nào. Nhấn "Thêm Header" nếu API yêu cầu Authorization hoặc Custom Headers.
            </div>
          } @else {
            <div class="space-y-2">
              @for (header of headers(); track $index) {
                <div class="flex items-center gap-2">
                  <input
                    type="text"
                    [value]="header.key"
                    (input)="updateHeaderKey($index, $any($event.target).value)"
                    placeholder="Header Key (vd: Authorization)"
                    class="flex-1 h-9 rounded-md border border-stroke bg-white px-3 text-xs font-mono text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-boxdark dark:text-white"
                  />
                  <input
                    type="text"
                    [value]="header.value"
                    (input)="updateHeaderValue($index, $any($event.target).value)"
                    placeholder="Header Value (hoặc Bearer token)"
                    class="flex-1 h-9 rounded-md border border-stroke bg-white px-3 text-xs font-mono text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-boxdark dark:text-white"
                  />
                  <button
                    type="button"
                    (click)="removeHeader($index)"
                    class="h-9 w-9 shrink-0 flex items-center justify-center rounded-md text-danger hover:bg-danger/10 transition-colors"
                    title="Xóa header này"
                  >
                    <app-icon name="trash" [size]="16" />
                  </button>
                </div>
              }
            </div>
          }
        </div>

        <!-- ── Nhóm 4: Parameters Schema JSON Editor ── -->
        <div class="space-y-2">
          <div class="flex items-center justify-between">
            <label class="text-xs font-semibold text-black dark:text-white flex items-center gap-2">
              <span>JSON Schema Tham số Đầu vào (Parameters Schema)</span>
              @if (schemaError()) {
                <span class="inline-flex items-center gap-1 text-[11px] text-danger font-normal">
                  <app-icon name="close" [size]="12" />
                  Cú pháp JSON không hợp lệ
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
              (click)="formatJsonSchema()"
              class="inline-flex items-center gap-1 text-xs text-primary hover:underline"
            >
              <app-icon name="code" [size]="14" />
              <span>Định dạng JSON</span>
            </button>
          </div>

          <textarea
            formControlName="parametersSchema"
            (input)="onSchemaChange()"
            rows="6"
            placeholder="{ ... }"
            class="w-full rounded-lg border font-mono text-xs p-3 outline-none transition-colors focus:border-primary dark:bg-boxdark dark:text-white custom-scrollbar"
            [class.border-danger]="schemaError()"
            [class.border-stroke]="!schemaError()"
            [class.dark:border-strokedark]="!schemaError()"
          ></textarea>

          @if (schemaError()) {
            <p class="text-[11px] text-danger font-mono">
              {{ schemaError() }}
            </p>
          }
        </div>

        <!-- ── Nhóm 5: Kiểm soát Thực thi & An toàn ── -->
        <div class="grid grid-cols-1 md:grid-cols-2 gap-4 items-center rounded-lg border border-stroke dark:border-strokedark p-4 bg-gray-2/20 dark:bg-meta-4/5">
          <!-- Requires Confirmation Gate -->
          <label class="flex items-start gap-3 cursor-pointer">
            <input
              type="checkbox"
              formControlName="requiresConfirmation"
              class="mt-1 h-4 w-4 rounded border-stroke text-primary focus:ring-primary dark:border-strokedark dark:bg-boxdark"
            />
            <div>
              <span class="text-xs font-semibold text-black dark:text-white block">
                Yêu cầu duyệt trước khi thực thi (Confirmation Gate)
              </span>
              <span class="text-[11px] text-body dark:text-bodydark block mt-0.5">
                Khi AI Agent kích hoạt công cụ này, hệ thống sẽ tạm dừng chờ người dùng xác nhận tham số trước khi gửi request.
              </span>
            </div>
          </label>

          <!-- Timeout Seconds -->
          <div>
            <label class="mb-1 block text-xs font-semibold text-black dark:text-white">
              Thời gian chờ tối đa (giây)
            </label>
            <input
              type="number"
              formControlName="timeoutSeconds"
              min="1"
              max="300"
              class="w-full h-10 rounded-lg border border-stroke bg-transparent px-3.5 text-sm text-black outline-none transition-colors focus:border-primary dark:border-strokedark dark:bg-meta-4 dark:text-white dark:focus:border-primary"
            />
            <span class="text-[11px] text-body dark:text-bodydark block mt-0.5">
              Từ 1 đến 300 giây (mặc định 30 giây).
            </span>
          </div>
        </div>
      </form>

      <!-- Modal Footer -->
      <div modal-footer class="flex items-center justify-end gap-3 w-full">
        <app-button
          variant="outline"
          size="md"
          [disabled]="isSubmitting()"
          (click)="onCancel()"
        >
          Hủy bỏ
        </app-button>

        <app-button
          variant="primary"
          size="md"
          [disabled]="isSubmitting() || !!schemaError() || form.invalid"
          (click)="onSubmit()"
        >
          @if (isSubmitting()) {
            <app-icon name="refresh" [size]="16" class="animate-spin mr-1.5" />
            <span>Đang lưu...</span>
          } @else {
            <app-icon name="check" [size]="16" class="mr-1.5" />
            <span>{{ isEditMode() ? 'Cập nhật Công cụ' : 'Tạo mới Công cụ' }}</span>
          }
        </app-button>
      </div>
    </app-modal>
  `,
})
export class ToolEditorComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly toolsService = inject(ToolsService);

  readonly tool = input<ToolDefinitionDto | null>(null);
  readonly isOpen = input<boolean>(true);
  readonly close = output<void>();
  readonly saved = output<ToolDefinitionDto>();

  readonly isEditMode = computed(() => !!this.tool());
  readonly modalTitle = computed(() =>
    this.isEditMode() ? 'Cập nhật Công cụ AI' : 'Thêm mới Công cụ AI (No-Code Builder)',
  );

  readonly isSubmitting = signal<boolean>(false);
  readonly errorMessage = signal<string | null>(null);
  readonly schemaError = signal<string | null>(null);
  readonly headers = signal<HeaderConfigItem[]>([]);

  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.pattern(/^[a-zA-Z0-9_-]+$/)]],
    displayName: ['', [Validators.required, Validators.minLength(2)]],
    description: ['', [Validators.required, Validators.minLength(5)]],
    httpMethod: ['GET' as HttpMethod, [Validators.required]],
    endpointUrl: ['', [Validators.required]],
    parametersSchema: [DEFAULT_SCHEMA_TEMPLATE],
    requiresConfirmation: [false],
    timeoutSeconds: [30, [Validators.required, Validators.min(1), Validators.max(300)]],
  });

  ngOnInit(): void {
    const current = this.tool();
    if (current) {
      this.form.patchValue({
        name: current.name,
        displayName: current.displayName,
        description: current.description,
        httpMethod: (current.httpMethod as HttpMethod) || 'GET',
        endpointUrl: current.endpointUrl,
        parametersSchema: current.parametersSchema || DEFAULT_SCHEMA_TEMPLATE,
        requiresConfirmation: current.requiresConfirmation,
        timeoutSeconds: current.timeoutSeconds || 30,
      });

      // Disable 'name' in edit mode
      this.form.get('name')?.disable();

      // Parse headersConfig
      if (current.headersConfig) {
        try {
          const parsed = JSON.parse(current.headersConfig);
          if (typeof parsed === 'object' && parsed !== null) {
            const items: HeaderConfigItem[] = Object.entries(parsed).map(([key, value]) => ({
              key,
              value: String(value),
            }));
            this.headers.set(items);
          }
        } catch {
          this.headers.set([]);
        }
      }
    }

    this.validateJsonSchema();
  }

  addHeader(): void {
    this.headers.update((current) => [...current, { key: '', value: '' }]);
  }

  removeHeader(index: number): void {
    this.headers.update((current) => current.filter((_, i) => i !== index));
  }

  updateHeaderKey(index: number, newKey: string): void {
    this.headers.update((current) =>
      current.map((item, i) => (i === index ? { ...item, key: newKey } : item)),
    );
  }

  updateHeaderValue(index: number, newValue: string): void {
    this.headers.update((current) =>
      current.map((item, i) => (i === index ? { ...item, value: newValue } : item)),
    );
  }

  onSchemaChange(): void {
    this.validateJsonSchema();
  }

  validateJsonSchema(): void {
    const raw = this.form.get('parametersSchema')?.value?.trim();
    if (!raw) {
      this.schemaError.set(null);
      return;
    }
    try {
      JSON.parse(raw);
      this.schemaError.set(null);
    } catch (e: any) {
      this.schemaError.set(e.message || 'Lỗi cú pháp JSON');
    }
  }

  formatJsonSchema(): void {
    const raw = this.form.get('parametersSchema')?.value?.trim();
    if (!raw) return;
    try {
      const parsed = JSON.parse(raw);
      const formatted = JSON.stringify(parsed, null, 2);
      this.form.patchValue({ parametersSchema: formatted });
      this.schemaError.set(null);
    } catch (e: any) {
      this.schemaError.set(e.message || 'Lỗi cú pháp JSON');
    }
  }

  onCancel(): void {
    this.close.emit();
  }

  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.validateJsonSchema();
    if (this.schemaError()) {
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    // Build headers JSON string
    const validHeaders = this.headers().filter((h) => h.key.trim().length > 0);
    let headersConfigJson: string | null = null;
    if (validHeaders.length > 0) {
      const headerObj: Record<string, string> = {};
      validHeaders.forEach((h) => {
        headerObj[h.key.trim()] = h.value;
      });
      headersConfigJson = JSON.stringify(headerObj);
    }

    const formRaw = this.form.getRawValue();

    if (this.isEditMode() && this.tool()) {
      const updateReq: UpdateToolRequest = {
        displayName: formRaw.displayName!,
        description: formRaw.description!,
        endpointUrl: formRaw.endpointUrl!,
        httpMethod: formRaw.httpMethod!,
        headersConfig: headersConfigJson,
        parametersSchema: formRaw.parametersSchema || null,
        requiresConfirmation: !!formRaw.requiresConfirmation,
        timeoutSeconds: Number(formRaw.timeoutSeconds) || 30,
      };

      this.toolsService.updateTool(this.tool()!.id, updateReq).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.success && res.data) {
            this.saved.emit(res.data);
            this.close.emit();
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(err?.detail || err?.message || 'Cập nhật công cụ thất bại.');
        },
      });
    } else {
      const createReq: CreateToolRequest = {
        name: formRaw.name!.trim(),
        displayName: formRaw.displayName!,
        description: formRaw.description!,
        type: 'RestApi',
        endpointUrl: formRaw.endpointUrl!,
        httpMethod: formRaw.httpMethod!,
        headersConfig: headersConfigJson,
        parametersSchema: formRaw.parametersSchema || null,
        requiresConfirmation: !!formRaw.requiresConfirmation,
        timeoutSeconds: Number(formRaw.timeoutSeconds) || 30,
      };

      this.toolsService.createTool(createReq).subscribe({
        next: (res) => {
          this.isSubmitting.set(false);
          if (res.success && res.data) {
            this.saved.emit(res.data);
            this.close.emit();
          }
        },
        error: (err) => {
          this.isSubmitting.set(false);
          this.errorMessage.set(err?.detail || err?.message || 'Tạo mới công cụ thất bại.');
        },
      });
    }
  }
}
