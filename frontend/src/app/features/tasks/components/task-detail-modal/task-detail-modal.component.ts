/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file task-detail-modal.component.ts — Modal xem chi tiết Tác vụ, Artifacts và Activity Logs (Sprint 2 - PBI-TASK-01)
 */

import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskDetail } from '../../models/task.model';
import { TasksHttpService } from '../../services/tasks-http.service';

@Component({
  selector: 'app-task-detail-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="modal-backdrop" (click)="close.emit()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <div>
            <div class="header-badges">
              <span class="task-code">{{ task.taskCode }}</span>
              <span class="status-badge" [class]="'status-' + task.status.toLowerCase()">{{ task.status }}</span>
              <span class="priority-badge" [class]="'priority-' + task.priority.toLowerCase()">{{ task.priority }}</span>
            </div>
            <h2>{{ task.title }}</h2>
          </div>
          <button type="button" class="btn-close" (click)="close.emit()">✕</button>
        </div>

        <div class="modal-body custom-scrollbar">
          <!-- Mô tả -->
          <div class="detail-section">
            <h4>Mô Tả Yêu Cầu</h4>
            <div class="desc-content">{{ task.description || 'Không có mô tả chi tiết.' }}</div>
          </div>

          <!-- Phân công -->
          <div class="detail-section">
            <h4>Phân Công Thực Thi</h4>
            <div class="assignments-list">
              @if (task.assignments.length === 0) {
                <span class="text-muted">Chưa có ai được phân công.</span>
              } @else {
                @for (a of task.assignments; track a.id) {
                  <div class="assignment-item">
                    <span class="avatar">{{ a.isAgent ? '🤖' : '👤' }}</span>
                    <span class="role">{{ a.roleInTask }}</span>
                    <span class="type-tag" [class.is-agent]="a.isAgent">{{ a.isAgent ? 'Nhân sự AI' : 'Con người' }}</span>
                  </div>
                }
              }
            </div>
          </div>

          <!-- Sản phẩm đầu ra (Artifacts) -->
          <div class="detail-section">
            <h4>Sản Phẩm Đầu Ra (Artifacts)</h4>
            @if (task.artifacts.length === 0) {
              <div class="empty-artifacts">Chưa có sản phẩm đầu ra nào được bàn giao.</div>
            } @else {
              <div class="artifacts-grid">
                @for (art of task.artifacts; track art.id) {
                  <div class="artifact-card">
                    <div class="art-icon">📄</div>
                    <div class="art-info">
                      <span class="art-name">{{ art.fileName }}</span>
                      <span class="art-meta">{{ (art.fileSize / 1024).toFixed(1) }} KB • {{ art.createdDate | date: 'dd/MM/yyyy HH:mm' }}</span>
                      @if (art.summary) {
                        <p class="art-summary">{{ art.summary }}</p>
                      }
                    </div>
                    <button
                      type="button"
                      class="btn-download"
                      (click)="onDownloadArtifact(art.id, art.fileName)"
                    >
                      Tải Về
                    </button>
                  </div>
                }
              </div>
            }
          </div>

          <!-- Lịch sử hoạt động (Activity Logs) -->
          <div class="detail-section">
            <h4>Lịch Sử Hoạt Động & Biến Động Trạng Thái</h4>
            <div class="timeline">
              @for (log of task.activityLogs; track log.id) {
                <div class="timeline-item">
                  <div class="timeline-dot"></div>
                  <div class="timeline-content">
                    <div class="log-header">
                      <span class="action-tag">{{ log.actionType }}</span>
                      <span class="log-time">{{ log.createdDate | date: 'dd/MM HH:mm:ss' }}</span>
                    </div>
                    @if (log.comment) {
                      <p class="log-comment">{{ log.comment }}</p>
                    }
                  </div>
                </div>
              }
            </div>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn-secondary" (click)="close.emit()">Đóng</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.45);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
    }

    .modal-card {
      background: #FFFFFF;
      width: 100%;
      max-width: 680px;
      max-height: 85vh;
      display: flex;
      flex-direction: column;
      border-radius: 12px;
      box-shadow: 0 10px 30px rgba(0, 0, 0, 0.2);
      border: 1px solid #E5E0D8;
      overflow: hidden;
    }

    .modal-header {
      padding: 16px 22px;
      border-bottom: 1px solid #EBE6DF;
      display: flex;
      justify-content: space-between;
      align-items: flex-start;

      .header-badges {
        display: flex;
        gap: 6px;
        align-items: center;
        margin-bottom: 6px;

        .task-code {
          font-family: monospace;
          font-size: 11px;
          font-weight: 700;
          color: #78716C;
        }

        .status-badge {
          font-size: 10px;
          font-weight: 700;
          padding: 2px 6px;
          border-radius: 4px;
          background: #E5E7EB;
          color: #374151;

          &.status-inprogress { background: #FEF3C7; color: #B45309; }
          &.status-inreview { background: #EEF2FF; color: #4338CA; }
          &.status-done { background: #D1FAE5; color: #065F46; }
        }

        .priority-badge {
          font-size: 10px;
          font-weight: 600;
          padding: 2px 6px;
          border-radius: 4px;
          background: #F3F4F6;
          color: #4B5563;
        }
      }

      h2 {
        margin: 0;
        font-size: 16px;
        font-weight: 700;
        color: #111827;
      }

      .btn-close {
        background: transparent;
        border: none;
        font-size: 18px;
        cursor: pointer;
        color: #9CA3AF;
        &:hover { color: #374151; }
      }
    }

    .modal-body {
      padding: 20px 22px;
      overflow-y: auto;
      flex: 1;

      .detail-section {
        margin-bottom: 20px;

        h4 {
          margin: 0 0 8px 0;
          font-size: 12px;
          font-weight: 700;
          text-transform: uppercase;
          letter-spacing: 0.05em;
          color: #6B7280;
        }

        .desc-content {
          font-size: 13px;
          color: #374151;
          line-height: 1.5;
          white-space: pre-wrap;
          background: #F9FAFB;
          border: 1px solid #E5E7EB;
          border-radius: 6px;
          padding: 10px 12px;
        }

        .assignments-list {
          display: flex;
          gap: 8px;
          flex-wrap: wrap;

          .assignment-item {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background: #F3F4F6;
            border: 1px solid #E5E7EB;
            border-radius: 6px;
            padding: 4px 10px;
            font-size: 12px;

            .type-tag {
              font-size: 10px;
              color: #6B7280;
              &.is-agent { color: #7C3AED; font-weight: 600; }
            }
          }
        }

        .empty-artifacts {
          font-size: 12px;
          color: #9CA3AF;
          font-style: italic;
        }

        .artifacts-grid {
          display: flex;
          flex-direction: column;
          gap: 8px;

          .artifact-card {
            display: flex;
            align-items: center;
            gap: 12px;
            background: #F8FAFC;
            border: 1px solid #E2E8F0;
            border-radius: 8px;
            padding: 10px 14px;

            .art-icon { font-size: 20px; }

            .art-info {
              flex: 1;
              .art-name { font-size: 13px; font-weight: 600; color: #1E293B; display: block; }
              .art-meta { font-size: 11px; color: #64748B; }
              .art-summary { font-size: 11px; color: #475569; margin: 4px 0 0 0; }
            }

            .btn-download {
              padding: 6px 12px;
              background: #0284C7;
              color: white;
              border: none;
              border-radius: 6px;
              font-size: 11px;
              font-weight: 600;
              cursor: pointer;
              &:hover { background: #0369A1; }
            }
          }
        }

        .timeline {
          position: relative;
          padding-left: 16px;
          border-left: 2px solid #E5E7EB;

          .timeline-item {
            position: relative;
            margin-bottom: 12px;

            .timeline-dot {
              position: absolute;
              left: -21px;
              top: 4px;
              width: 8px;
              height: 8px;
              border-radius: 50%;
              background: #9CA3AF;
            }

            .timeline-content {
              .log-header {
                display: flex;
                align-items: center;
                gap: 8px;
                .action-tag { font-size: 11px; font-weight: 700; color: #374151; }
                .log-time { font-size: 10px; color: #9CA3AF; }
              }
              .log-comment {
                margin: 2px 0 0 0;
                font-size: 11px;
                color: #4B5563;
              }
            }
          }
        }
      }
    }

    .modal-footer {
      padding: 12px 22px;
      background: #F9FAFB;
      border-top: 1px solid #E5E7EB;
      display: flex;
      justify-content: flex-end;

      button {
        padding: 8px 16px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
        background: #FFFFFF;
        border: 1px solid #D1D5DB;
        color: #374151;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskDetailModalComponent {
  @Input({ required: true }) task!: TaskDetail;
  @Output() close = new EventEmitter<void>();

  private readonly http = inject(TasksHttpService);

  onDownloadArtifact(artifactId: string, fileName: string): void {
    this.http.downloadArtifact(this.task.id, artifactId).subscribe((blob) => {
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();
      window.URL.revokeObjectURL(url);
    });
  }
}
