/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file agent-profile-modal.component.ts — Modal tạo mới cấu hình hồ sơ Agent AI (Sprint 0 - PBI-ORG-02)
 *
 * ## Thiết Kế Khởi Tạo Agent Tự Chủ
 * Cung cấp biểu mẫu trực quan cho phép người dùng cấu hình nhân viên AI:
 * Tên Agent, LLM Provider (Gemini, OpenAI, Claude, Ollama), Model Name,
 * Temperature (0.0 - 2.0), System Prompt và Ghi chú bộ nhớ riêng (Private Memory).
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05, RULE-06)
 * 🔴 OnPush: Áp dụng ChangeDetectionStrategy.OnPush, cập nhật giao diện hoàn toàn qua Signals.
 * ⛔ Cấm submit form khi form còn trường lỗi (`form.invalid`), đánh dấu touched toàn bộ để hiển thị cảnh báo.
 * ⇒ Khi chọn Provider thay đổi ⇒ Tự động điền model mặc định tối ưu tương ứng vào trường `modelName`.
 * AI-NOTE: Đóng modal sẽ tự động reset toàn bộ các giá trị trong FormGroup về trạng thái chuẩn ban đầu.
 */

import {
  Component,
  ChangeDetectionStrategy,
  EventEmitter,
  Input,
  Output,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ModelProviderType, CreateAgentProfileRequest } from '../../models/organization.models';
import { OrganizationStore } from '../../store/organization.store';

@Component({
  selector: 'app-agent-profile-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule],
  templateUrl: './agent-profile-modal.component.html',
  styleUrls: ['./agent-profile-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentProfileModalComponent {
  // ── Injections ──
  private readonly fb = inject(FormBuilder);
  readonly store = inject(OrganizationStore);

  // ── Inputs & Outputs ──
  @Input() isOpen = false;
  @Output() closed = new EventEmitter<void>();
  @Output() created = new EventEmitter<void>();

  // ── State Signals & Constants ──
  readonly ModelProviderType = ModelProviderType;
  readonly submitting = signal<boolean>(false);
  readonly submitError = signal<string | null>(null);

  // ── Providers Danh Mục ──
  readonly providers = [
    { type: ModelProviderType.Gemini, name: 'Google Gemini', tag: 'Gemini', defaultModel: 'gemini-2.5-flash' },
    { type: ModelProviderType.OpenAI, name: 'OpenAI GPT', tag: 'OpenAI', defaultModel: 'gpt-4o' },
    { type: ModelProviderType.Claude, name: 'Anthropic Claude', tag: 'Claude', defaultModel: 'claude-3-7-sonnet' },
    { type: ModelProviderType.Ollama, name: 'Ollama Local', tag: 'Ollama', defaultModel: 'deepseek-r1:7b' },
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
  });

  // ── Getters & Event Handlers ──

  /**
   * Chuyển đổi nhà cung cấp LLM và tự động cập nhật model mặc định tương ứng.
   * ⇒ Thay đổi provider ⇒ patchValue `modelName` bằng `defaultModel`.
   */
  onProviderChange(providerType: ModelProviderType): void {
    this.form.patchValue({ provider: providerType });
    const selected = this.providers.find((p) => p.type === providerType);
    if (selected) {
      this.form.patchValue({ modelName: selected.defaultModel });
    }
  }

  /**
   * Lấy giá trị nhiệt độ (temperature) dạng số từ form control.
   */
  get temperatureValue(): number {
    return Number(this.form.get('temperature')?.value ?? 0.7);
  }

  /**
   * Đóng modal và reset biểu mẫu về trạng thái ban đầu.
   */
  close(): void {
    this.isOpen = false;
    this.form.reset({
      name: '',
      provider: ModelProviderType.Gemini,
      modelName: 'gemini-2.5-flash',
      systemPrompt:
        'Bạn là một trợ lý AI thông minh, phối hợp cùng các thành viên trong kênh để giải quyết công việc hiệu quả.',
      temperature: 0.7,
      privateMemoryNotes: '',
      avatarUrl: '',
    });
    this.submitError.set(null);
    this.closed.emit();
  }

  /**
   * Thực hiện lưu hồ sơ Agent AI mới lên server.
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const val = this.form.value;
    const req: CreateAgentProfileRequest = {
      name: val.name!.trim(),
      provider: Number(val.provider) as ModelProviderType,
      modelName: val.modelName!.trim(),
      systemPrompt: val.systemPrompt!.trim(),
      temperature: Number(val.temperature),
      privateMemoryNotes: val.privateMemoryNotes?.trim() || undefined,
      avatarUrl: val.avatarUrl?.trim() || undefined,
    };

    this.submitting.set(true);
    this.submitError.set(null);

    this.store.createAgentProfile(req).subscribe({
      next: () => {
        this.submitting.set(false);
        this.created.emit();
        this.close();
      },
      error: (err) => {
        this.submitting.set(false);
        this.submitError.set(err?.detail || err?.message || 'Có lỗi xảy ra khi tạo hồ sơ Agent');
      },
    });
  }
}
