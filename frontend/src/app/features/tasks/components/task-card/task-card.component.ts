/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file task-card.component.ts — Thẻ hiển thị tác vụ trên bảng Kanban (Sprint 2 - PBI-TASK-01)
 */

import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TaskItem } from '../../models/task.model';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      class="task-card"
      [class.in-review]="task.status === 'InReview'"
      [class.in-progress]="task.status === 'InProgress'"
      (click)="viewDetails.emit(task)"
    >
      <!-- Running Badge for Hangfire -->
      @if (task.status === 'InProgress' && hasAgentAssignee()) {
        <div class="running-indicator">
          <span class="pulse-dot"></span>
          <span>⚡ HANGFIRE RUNNING</span>
        </div>
      }

      <div class="card-header">
        <span class="task-code">{{ task.taskCode }}</span>
        <span class="priority-badge" [class]="'priority-' + task.priority.toLowerCase()">
          {{ task.priority }}
        </span>
      </div>

      <h4 class="task-title">{{ task.title }}</h4>

      @if (task.description) {
        <p class="task-desc">{{ task.description }}</p>
      }

      <!-- Review Gate Action Box -->
      @if (task.status === 'InReview') {
        <div class="review-gate-box" (click)="$event.stopPropagation()">
          <span class="gate-label">CỔNG NGHIỆM THU (REVIEW GATE)</span>
          <div class="gate-actions">
            <button
              type="button"
              class="btn-approve"
              (click)="reviewGate.emit({ task, isApproved: true })"
            >
              ✓ Duyệt
            </button>
            <button
              type="button"
              class="btn-revise"
              (click)="reviewGate.emit({ task, isApproved: false })"
            >
              ↺ Yêu cầu sửa
            </button>
          </div>
        </div>
      }

      <div class="card-footer">
        <div class="assignee-box">
          @if (task.assignments.length > 0) {
            @for (assign of task.assignments; track assign.id) {
              @if (assign.isAgent) {
                <span class="avatar-agent" title="Nhân viên số AI: {{ assign.roleInTask }}">
                  🤖
                </span>
              } @else {
                <span class="avatar-human" title="Thành viên con người: {{ assign.roleInTask }}">
                  👤
                </span>
              }
            }
          } @else {
            <span class="unassigned-text">Chưa phân công</span>
          }
        </div>

        @if (task.dueDate) {
          <span class="due-date">
            Hạn: {{ task.dueDate | date: 'dd/MM' }}
          </span>
        }
      </div>
    </div>
  `,
  styles: [`
    .task-card {
      background: #FFFFFF;
      border: 1px solid #E2E8F0;
      border-radius: 6px;
      padding: 12px;
      margin-bottom: 10px;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
      box-shadow: 0 1px 2px rgba(0, 0, 0, 0.04);
      position: relative;
      overflow: hidden;

      :host-context(.dark) & {
        background: #1a222c;
        border-color: #2e3a47;
      }

      &:hover {
        border-color: #C8C3BA;
        box-shadow: 0 3px 6px rgba(0, 0, 0, 0.06);
      }

      &.in-progress {
        border-left: 3px solid #F59E0B;
      }

      &.in-review {
        border: 1px solid #818CF8;
        background: #FDFEFE;

        :host-context(.dark) & {
          background: #24303f;
        }
      }
    }

    .running-indicator {
      display: flex;
      align-items: center;
      gap: 5px;
      font-size: 9px;
      font-weight: 700;
      color: #B45309;
      background: #FEF3C7;
      padding: 2px 6px;
      border-radius: 4px;
      margin-bottom: 8px;
      width: fit-content;

      .pulse-dot {
        width: 6px;
        height: 6px;
        border-radius: 50%;
        background: #D97706;
        animation: pulse 1.5s infinite;
      }
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; transform: scale(1); }
      50% { opacity: 0.4; transform: scale(1.2); }
    }

    .card-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 6px;

      .task-code {
        font-family: monospace;
        font-size: 11px;
        font-weight: 600;
        color: #78716C;
      }

      .priority-badge {
        font-size: 10px;
        font-weight: 600;
        padding: 2px 6px;
        border-radius: 4px;

        &.priority-low { background: #F3F4F6; color: #4B5563; }
        &.priority-medium { background: #EFF6FF; color: #1D4ED8; }
        &.priority-high { background: #FEF3C7; color: #B45309; }
        &.priority-urgent { background: #FEE2E2; color: #B91C1C; }
      }
    }

    .task-title {
      font-size: 13px;
      font-weight: 600;
      color: #1F2937;
      margin: 0 0 6px 0;
      line-height: 1.35;

      :host-context(.dark) & {
        color: #ffffff;
      }
    }

    .task-desc {
      font-size: 11px;
      color: #6B7280;
      margin: 0 0 8px 0;
      display: -webkit-box;
      -webkit-line-clamp: 2;
      -webkit-box-orient: vertical;
      overflow: hidden;
      line-height: 1.4;

      :host-context(.dark) & {
        color: #aeb7c0;
      }
    }

    .review-gate-box {
      background: #EEF2FF;
      border: 1px solid #C7D2FE;
      border-radius: 6px;
      padding: 8px;
      margin-bottom: 8px;

      .gate-label {
        display: block;
        font-size: 9px;
        font-weight: 800;
        color: #4338CA;
        margin-bottom: 6px;
      }

      .gate-actions {
        display: flex;
        gap: 6px;

        button {
          flex: 1;
          padding: 4px 6px;
          border-radius: 4px;
          font-size: 11px;
          font-weight: 600;
          cursor: pointer;
          border: none;
          transition: background 0.1s;
        }

        .btn-approve {
          background: #059669;
          color: #FFFFFF;
          &:hover { background: #047857; }
        }

        .btn-revise {
          background: #FFFFFF;
          color: #B45309;
          border: 1px solid #FCD34D;
          &:hover { background: #FFFBEB; }
        }
      }
    }

    .card-footer {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding-top: 8px;
      border-top: 1px solid #F3F1EC;

      .assignee-box {
        display: flex;
        gap: 4px;

        .avatar-agent, .avatar-human {
          display: inline-flex;
          align-items: center;
          justify-content: center;
          width: 22px;
          height: 22px;
          border-radius: 50%;
          font-size: 11px;
          background: #F3F4F6;
          border: 1px solid #E5E7EB;
        }

        .unassigned-text {
          font-size: 10px;
          font-style: italic;
          color: #9CA3AF;
        }
      }

      .due-date {
        font-size: 10px;
        color: #9CA3AF;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskCardComponent {
  @Input({ required: true }) task!: TaskItem;
  @Output() viewDetails = new EventEmitter<TaskItem>();
  @Output() reviewGate = new EventEmitter<{ task: TaskItem; isApproved: boolean }>();

  hasAgentAssignee(): boolean {
    return this.task.assignments.some((a) => a.isAgent);
  }
}
