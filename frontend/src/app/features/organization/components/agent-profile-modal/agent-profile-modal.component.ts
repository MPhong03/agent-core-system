/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
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
  private readonly fb = inject(FormBuilder);
  readonly store = inject(OrganizationStore);

  @Input() isOpen = false;
  @Output() closed = new EventEmitter<void>();
  @Output() created = new EventEmitter<void>();

  readonly ModelProviderType = ModelProviderType;
  readonly submitting = signal<boolean>(false);
  readonly submitError = signal<string | null>(null);

  readonly providers = [
    { type: ModelProviderType.Gemini, name: 'Google Gemini', tag: 'Gemini', defaultModel: 'gemini-2.5-flash' },
    { type: ModelProviderType.OpenAI, name: 'OpenAI GPT', tag: 'OpenAI', defaultModel: 'gpt-4o' },
    { type: ModelProviderType.Claude, name: 'Anthropic Claude', tag: 'Claude', defaultModel: 'claude-3-7-sonnet' },
    { type: ModelProviderType.Ollama, name: 'Ollama Local', tag: 'Ollama', defaultModel: 'deepseek-r1:7b' },
  ];

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

  onProviderChange(providerType: ModelProviderType): void {
    this.form.patchValue({ provider: providerType });
    const selected = this.providers.find((p) => p.type === providerType);
    if (selected) {
      this.form.patchValue({ modelName: selected.defaultModel });
    }
  }

  get temperatureValue(): number {
    return Number(this.form.get('temperature')?.value ?? 0.7);
  }

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
