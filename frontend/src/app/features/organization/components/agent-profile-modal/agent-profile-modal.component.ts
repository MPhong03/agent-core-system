/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file agent-profile-modal.component.ts — Modal tạo mới và cấu hình hồ sơ Agent AI (Sprint 3 - PBI-TOOL-02)
 *
 * ## Thiết Kế Khởi Tạo & Cấu Hình Agent Tự Chủ
 * Cung cấp biểu mẫu trực quan cho phép người dùng cấu hình nhân viên AI:
 * Tên Agent, LLM Provider (Gemini, OpenAI, Claude, Ollama), Model Name,
 * Temperature (0.0 - 2.0), System Prompt, Ghi chú bộ nhớ riêng (Private Memory),
 * Chế độ thực thi công cụ (ToolExecutionMode: Auto, SemiAuto, NeedApprove),
 * và Phân quyền công cụ (Assigned Tools) tích hợp Function Calling.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05, RULE-06)
 * 🔴 OnPush: Áp dụng ChangeDetectionStrategy.OnPush, cập nhật giao diện hoàn toàn qua Signals.
 * ⛔ Cấm submit form khi form còn trường lỗi (`form.invalid`), đánh dấu touched toàn bộ để hiển thị cảnh báo.
 * ⇒ Khi chọn Provider thay đổi ⇒ Tự động điền model mặc định tối ưu tương ứng vào trường `modelName`.
 * ⇒ Hỗ trợ 2 chế độ: Tạo hồ sơ Agent mới hoặc Cấu hình lại phân quyền công cụ cho Agent hiện có (agentProfileId).
 * AI-NOTE: Đóng modal sẽ tự động reset toàn bộ các giá trị trong FormGroup về trạng thái chuẩn ban đầu.
 */

import {
  Component,
  ChangeDetectionStrategy,
  EventEmitter,
  Input,
  Output,
  OnInit,
  OnChanges,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import {
  ModelProviderType,
  ToolExecutionMode,
  AgentProfile,
  CreateAgentProfileRequest,
} from '../../models/organization.models';
import { OrganizationStore } from '../../store/organization.store';
import { ToolsService } from '../../../agents/tools/services/tools.service';

@Component({
  selector: 'app-agent-profile-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './agent-profile-modal.component.html',
  styleUrls: ['./agent-profile-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentProfileModalComponent implements OnInit, OnChanges {
  // ── Injections ──
  private readonly fb = inject(FormBuilder);
  readonly store = inject(OrganizationStore);
  readonly toolsService = inject(ToolsService);

  // ── Inputs & Outputs ──
  @Input() isOpen = false;
  @Input() agentProfileId?: string;
  @Input() agentProfile?: AgentProfile | null;
  @Output() closed = new EventEmitter<void>();
  @Output() created = new EventEmitter<void>();

  // ── State Signals & Constants ──
  readonly ModelProviderType = ModelProviderType;
  readonly ToolExecutionMode = ToolExecutionMode;
  readonly submitting = signal<boolean>(false);
  readonly submitError = signal<string | null>(null);

  // ── Providers Danh Mục ──
  readonly providers = [
    { type: ModelProviderType.Gemini, name: 'Google Gemini', tag: 'Gemini', defaultModel: 'gemini-2.5-flash' },
    { type: ModelProviderType.OpenAI, name: 'OpenAI GPT', tag: 'OpenAI', defaultModel: 'gpt-4o' },
    { type: ModelProviderType.Claude, name: 'Anthropic Claude', tag: 'Claude', defaultModel: 'claude-3-7-sonnet' },
    { type: ModelProviderType.Ollama, name: 'Ollama Local', tag: 'Ollama', defaultModel: 'deepseek-r1:7b' },
  ];

  // ── Tool Execution Modes Danh Mục ──
  readonly executionModes = [
    {
      mode: ToolExecutionMode.Auto,
      name: 'Auto (Tự trị)',
      tag: 'Tự động',
      isRecommended: false,
      description: 'Tự động chạy tất cả công cụ',
    },
    {
      mode: ToolExecutionMode.SemiAuto,
      name: 'Semi-Auto (Bán tự động)',
      tag: 'Khuyến nghị',
      isRecommended: true,
      description: 'Tự chạy công cụ an toàn, hỏi phê duyệt với thao tác nhạy cảm',
    },
    {
      mode: ToolExecutionMode.NeedApprove,
      name: 'Need Approve (Nghiêm ngặt)',
      tag: 'Nghiêm ngặt',
      isRecommended: false,
      description: 'Luôn hỏi phê duyệt trước mọi lượt gọi công cụ',
    },
  ];

  // ── Form Định Nghĩa ──
  readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
    provider: [ModelProviderType.Gemini, [Validators.required]],
    modelName: ['gemini-2.5-flash', [Validators.required, Validators.maxLength(100)]],
    systemPrompt: [
      'Bạn là một trợ lý AI thông minh, phối hợp cùng các thành viên trong kênh để giải quyết công việc hiệu quả.',
      [Validators.required],
    ],
    temperature: [0.7, [Validators.required, Validators.min(0.0), Validators.max(2.0)]],
    privateMemoryNotes: [''],
    avatarUrl: [''],
    toolExecutionMode: [ToolExecutionMode.SemiAuto, [Validators.required]],
    selectedToolIds: [[] as string[]],
  });

  // ── Getters ──

  get isEditMode(): boolean {
    return !!(this.agentProfileId || this.agentProfile?.id);
  }

  get activeAgentId(): string | undefined {
    return this.agentProfileId || this.agentProfile?.id;
  }

  get temperatureValue(): number {
    return Number(this.form.get('temperature')?.value ?? 0.7);
  }

  // ── Vòng Đời Lifecycle ──

  ngOnInit(): void {
    if (this.isOpen) {
      this.initModalData();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (
      changes['isOpen']?.currentValue === true ||
      changes['agentProfileId'] ||
      changes['agentProfile']
    ) {
      if (this.isOpen) {
        this.initModalData();
      }
    }
  }

  private initModalData(): void {
    this.toolsService.loadTools().subscribe();

    const activeId = this.activeAgentId;
    if (activeId) {
      this.form.get('toolExecutionMode')?.disable();

      if (this.agentProfile) {
        this.form.patchValue({
          name: this.agentProfile.name,
          provider: this.agentProfile.provider,
          modelName: this.agentProfile.modelName,
          systemPrompt: this.agentProfile.systemPrompt,
          temperature: this.agentProfile.temperature,
          privateMemoryNotes: this.agentProfile.privateMemoryNotes || '',
          avatarUrl: this.agentProfile.avatarUrl || '',
          toolExecutionMode: this.agentProfile.toolExecutionMode ?? ToolExecutionMode.SemiAuto,
        });
      }

      this.toolsService.getAgentTools(activeId).subscribe({
        next: (res) => {
          if (res.success && res.data) {
            this.form.patchValue({
              selectedToolIds: res.data.map((t) => t.id),
            });
          }
        },
      });
    } else {
      this.form.get('toolExecutionMode')?.enable();
    }
  }

  // ── Event Handlers ──

  /**
   * Chuyển đổi nhà cung cấp LLM và tự động cập nhật model mặc định tương ứng.
   */
  onProviderChange(providerType: ModelProviderType): void {
    this.form.patchValue({ provider: providerType });
    const selected = this.providers.find((p) => p.type === providerType);
    if (selected) {
      this.form.patchValue({ modelName: selected.defaultModel });
    }
  }

  /**
   * Thay đổi chế độ thực thi công cụ cho Agent (chỉ khả dụng khi tạo mới).
   */
  onModeChange(mode: ToolExecutionMode): void {
    if (this.isEditMode) {
      return;
    }
    this.form.patchValue({ toolExecutionMode: mode });
  }

  /**
   * Kiểm tra xem một công cụ đã được cấp quyền cho Agent hay chưa.
   */
  isToolSelected(toolId: string): boolean {
    const current = (this.form.get('selectedToolIds')?.value as string[]) || [];
    return current.includes(toolId);
  }

  /**
   * Bật hoặc tắt phân quyền một công cụ cụ thể cho Agent.
   */
  toggleTool(toolId: string): void {
    const current = (this.form.get('selectedToolIds')?.value as string[]) || [];
    const next = current.includes(toolId)
      ? current.filter((id) => id !== toolId)
      : [...current, toolId];

    this.form.patchValue({ selectedToolIds: next });
    this.form.get('selectedToolIds')?.markAsDirty();
  }

  /**
   * Đóng modal và reset biểu mẫu về trạng thái ban đầu.
   */
  close(): void {
    this.isOpen = false;
    this.form.get('toolExecutionMode')?.enable();
    this.form.reset({
      name: '',
      provider: ModelProviderType.Gemini,
      modelName: 'gemini-2.5-flash',
      systemPrompt:
        'Bạn là một trợ lý AI thông minh, phối hợp cùng các thành viên trong kênh để giải quyết công việc hiệu quả.',
      temperature: 0.7,
      privateMemoryNotes: '',
      avatarUrl: '',
      toolExecutionMode: ToolExecutionMode.SemiAuto,
      selectedToolIds: [],
    });
    this.submitError.set(null);
    this.closed.emit();
  }

  /**
   * Thực hiện lưu hoặc cấu hình hồ sơ Agent AI.
   */
  onSubmit(): void {
    const activeId = this.activeAgentId;
    const val = this.form.value;
    const selectedToolIds = (val.selectedToolIds as string[]) || [];

    if (activeId) {
      // Chế độ cấu hình lại công cụ cho Agent đã tồn tại
      this.submitting.set(true);
      this.submitError.set(null);

      this.toolsService.assignAgentTools(activeId, selectedToolIds).subscribe({
        next: () => {
          this.submitting.set(false);
          this.created.emit();
          this.close();
        },
        error: (err) => {
          this.submitting.set(false);
          this.submitError.set(
            err?.detail || err?.message || 'Có lỗi xảy ra khi cấu hình công cụ cho Agent',
          );
        },
      });
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    // Chế độ khởi tạo hồ sơ Agent mới kèm ToolExecutionMode và phân quyền Tools
    const req: CreateAgentProfileRequest = {
      name: val.name!.trim(),
      provider: Number(val.provider) as ModelProviderType,
      modelName: val.modelName!.trim(),
      systemPrompt: val.systemPrompt!.trim(),
      temperature: Number(val.temperature),
      privateMemoryNotes: val.privateMemoryNotes?.trim() || undefined,
      avatarUrl: val.avatarUrl?.trim() || undefined,
      toolExecutionMode: Number(val.toolExecutionMode) as ToolExecutionMode,
    };

    this.store.createAgentProfile(req).subscribe({
      next: (res) => {
        const createdAgentId = res?.data;
        if (createdAgentId && selectedToolIds.length > 0) {
          this.toolsService.assignAgentTools(createdAgentId, selectedToolIds).subscribe({
            next: () => {
              this.submitting.set(false);
              this.created.emit();
              this.close();
            },
            error: (err) => {
              this.submitting.set(false);
              this.submitError.set(
                err?.detail || err?.message || 'Tạo hồ sơ thành công nhưng lỗi khi gán công cụ',
              );
            },
          });
        } else {
          this.submitting.set(false);
          this.created.emit();
          this.close();
        }
      },
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(err?.detail || err?.message || 'Có lỗi xảy ra khi tạo hồ sơ Agent');
      },
    });
  }
}
