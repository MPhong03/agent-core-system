/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file agent-workspace.component.ts — Quản lý Workspace & Sandbox Python của Agent AI (Sprint 4)
 *
 * ## Kiến trúc
 * Cung cấp giao diện trực quan để giám sát môi trường làm việc cô lập của Agent:
 * - Trạng thái Virtualenv (.venv) & Khởi tạo môi trường.
 * - Quản lý cài đặt gói phụ thuộc (pip packages).
 * - File Explorer phân loại Scripts, Artifacts, Logs.
 * - Live Script Runner thử nghiệm code Python trong sandbox.
 * - Lịch sử thực thi script và Artifacts Preview.
 *
 * ## Ràng buộc Kỹ thuật & Anti-AI-Slop (RULE-01, RULE-05, RULE-06, RULE-07)
 * 🔴 OnPush: Áp dụng ChangeDetectionStrategy.OnPush, phản ứng tức thì qua Signals.
 * ⛔ Không để lại bất kỳ mock dữ liệu nào, tương tác trực tiếp qua AgentWorkspaceService.
 */

import {
  Component,
  ChangeDetectionStrategy,
  Input,
  OnInit,
  OnChanges,
  SimpleChanges,
  inject,
  signal,
  computed,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AgentWorkspaceService } from '../../services/agent-workspace.service';
import { WorkspaceFile, ScriptExecutionHistory } from '../../models/workspace.models';
import { ArtifactPreviewModalComponent } from '../artifact-preview-modal/artifact-preview-modal.component';

@Component({
  selector: 'app-agent-workspace',
  standalone: true,
  imports: [CommonModule, FormsModule, ArtifactPreviewModalComponent],
  templateUrl: './agent-workspace.component.html',
  styleUrls: ['./agent-workspace.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AgentWorkspaceComponent implements OnInit, OnChanges {
  readonly workspaceService = inject(AgentWorkspaceService);

  @Input({ required: true }) agentProfileId!: string;

  // ── Local State Signals ──
  readonly selectedCategory = signal<string>('all');
  readonly newPackageInput = signal<string>('');
  readonly testScriptCode = signal<string>(
    'import sys\nprint(f"Python Sandbox Version: {sys.version}")\nwith open("artifacts/test_output.txt", "w") as f:\n    f.write("Generated from Agent Sandbox!")\nprint("Artifact test_output.txt created successfully.")'
  );
  readonly activeTab = signal<'files' | 'packages' | 'runner' | 'history'>('files');

  // ── Preview Modal State ──
  readonly isPreviewOpen = signal<boolean>(false);
  readonly previewFile = signal<WorkspaceFile | null>(null);

  // ── Notification Messages ──
  readonly feedbackMessage = signal<{ text: string; type: 'success' | 'error' } | null>(null);

  // ── Computed Properties ──
  readonly filteredFiles = computed(() => {
    const files = this.workspaceService.files();
    const cat = this.selectedCategory();
    if (cat === 'all') return files;
    return files.filter((f) => f.category.toLowerCase() === cat.toLowerCase());
  });

  ngOnInit(): void {
    if (this.agentProfileId) {
      this.loadAllWorkspaceData();
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['agentProfileId'] && !changes['agentProfileId'].isFirstChange() && this.agentProfileId) {
      this.loadAllWorkspaceData();
    }
  }

  loadAllWorkspaceData(): void {
    this.workspaceService.getWorkspace(this.agentProfileId).subscribe({
      error: () => this.setFeedback('Không thể nạp thông tin workspace.', 'error'),
    });
    this.workspaceService.listFiles(this.agentProfileId).subscribe();
    this.workspaceService.getPackages(this.agentProfileId).subscribe();
    this.workspaceService.getHistory(this.agentProfileId).subscribe();
  }

  onInitVenv(): void {
    if (!this.agentProfileId) return;
    this.workspaceService.initVenv(this.agentProfileId).subscribe({
      next: () => {
        this.setFeedback('Khởi tạo môi trường ảo Python (.venv) thành công!', 'success');
      },
      error: (err) => {
        const msg = err.error?.detail || err.message || 'Lỗi khi khởi tạo môi trường .venv.';
        this.setFeedback(msg, 'error');
      },
    });
  }

  onInstallPackage(): void {
    const input = this.newPackageInput().trim();
    if (!input || !this.agentProfileId) return;

    const pkgs = input.split(/\s+/).filter(Boolean);
    this.workspaceService.installPackages(this.agentProfileId, pkgs).subscribe({
      next: () => {
        this.setFeedback(`Đã gửi lệnh cài đặt gói: ${pkgs.join(', ')}`, 'success');
        this.newPackageInput.set('');
      },
      error: (err) => {
        const msg = err.error?.detail || err.message || 'Lỗi cài đặt package.';
        this.setFeedback(msg, 'error');
      },
    });
  }

  onRunScript(): void {
    const code = this.testScriptCode().trim();
    if (!code || !this.agentProfileId) return;

    this.workspaceService.executeScript(this.agentProfileId, code, 'interactive_test.py').subscribe({
      next: (res) => {
        if (res.data?.isSuccess) {
          this.setFeedback('Chạy script thành công!', 'success');
        } else {
          this.setFeedback('Script thực thi kết thúc với lỗi (Exit code khác 0).', 'error');
        }
      },
      error: (err) => {
        const msg = err.error?.detail || err.message || 'Lỗi hệ thống khi thực thi script.';
        this.setFeedback(msg, 'error');
      },
    });
  }

  openPreview(file: WorkspaceFile): void {
    this.previewFile.set(file);
    this.isPreviewOpen.set(true);
  }

  closePreview(): void {
    this.isPreviewOpen.set(false);
    this.previewFile.set(null);
  }

  setCategory(category: string): void {
    this.selectedCategory.set(category);
  }

  setActiveTab(tab: 'files' | 'packages' | 'runner' | 'history'): void {
    this.activeTab.set(tab);
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  private setFeedback(text: string, type: 'success' | 'error'): void {
    this.feedbackMessage.set({ text, type });
    setTimeout(() => {
      this.feedbackMessage.set(null);
    }, 4500);
  }
}
