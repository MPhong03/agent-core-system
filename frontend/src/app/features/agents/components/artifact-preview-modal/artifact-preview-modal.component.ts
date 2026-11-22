/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import {
  Component,
  ChangeDetectionStrategy,
  Input,
  Output,
  EventEmitter,
  inject,
  signal,
  OnChanges,
  SimpleChanges,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { WorkspaceFile } from '../../models/workspace.models';
import { AgentWorkspaceService } from '../../services/agent-workspace.service';

@Component({
  selector: 'app-artifact-preview-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './artifact-preview-modal.component.html',
  styleUrls: ['./artifact-preview-modal.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ArtifactPreviewModalComponent implements OnChanges {
  private readonly workspaceService = inject(AgentWorkspaceService);
  private readonly http = inject(HttpClient);

  @Input() isOpen = false;
  @Input() file: WorkspaceFile | null = null;
  @Input() agentProfileId = '';

  @Output() closed = new EventEmitter<void>();

  readonly textContent = signal<string | null>(null);
  readonly isLoadingContent = signal<boolean>(false);

  ngOnChanges(changes: SimpleChanges): void {
    if ((changes['file'] || changes['isOpen']) && this.isOpen && this.file && this.agentProfileId) {
      if (this.isTextBased(this.file.extension)) {
        this.loadTextContent();
      } else {
        this.textContent.set(null);
      }
    }
  }

  close(): void {
    this.closed.emit();
  }

  getDownloadUrl(): string {
    if (!this.file || !this.agentProfileId) return '';
    return this.workspaceService.getDownloadUrl(this.agentProfileId, this.file.relativePath);
  }

  isImage(extension: string): boolean {
    const ext = extension.toLowerCase();
    return ext === '.png' || ext === '.jpg' || ext === '.jpeg' || ext === '.svg' || ext === '.webp';
  }

  isTextBased(extension: string): boolean {
    const ext = extension.toLowerCase();
    return (
      ext === '.csv' ||
      ext === '.json' ||
      ext === '.txt' ||
      ext === '.py' ||
      ext === '.md' ||
      ext === '.log' ||
      ext === '.html'
    );
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  private loadTextContent(): void {
    if (!this.file || !this.agentProfileId) return;

    this.isLoadingContent.set(true);
    const url = this.getDownloadUrl();
    this.http.get(url, { responseType: 'text' }).subscribe({
      next: (content) => {
        this.textContent.set(content);
        this.isLoadingContent.set(false);
      },
      error: () => {
        this.textContent.set('Không thể tải nội dung tệp tin.');
        this.isLoadingContent.set(false);
      },
    });
  }
}
