/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file task-create-modal.component.ts — Modal tạo tác vụ mới trên Kanban (Sprint 2 - PBI-TASK-01)
 */

import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CreateTaskRequest, TaskPriority } from '../../models/task.model';

export interface ChannelMemberOption {
  id: string;
  displayName: string;
  isAgent: boolean;
}

@Component({
  selector: 'app-task-create-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop" (click)="close.emit()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <h3>Tạo Tác Vụ Mới</h3>
          <button type="button" class="btn-close" (click)="close.emit()">✕</button>
        </div>

        <form (ngSubmit)="onSubmit()">
          <div class="modal-body">
            <div class="form-group">
              <label for="title">Tiêu đề tác vụ *</label>
              <input
                id="title"
                type="text"
                [(ngModel)]="title"
                (input)="title = $any($event.target).value"
                name="title"
                required
                placeholder="Nhập tên công việc cần thực hiện..."
              />
            </div>

            <div class="form-group">
              <label for="description">Mô tả chi tiết</label>
              <textarea
                id="description"
                rows="4"
                [(ngModel)]="description"
                (input)="description = $any($event.target).value"
                name="description"
                placeholder="Mô tả cụ thể các yêu cầu hoặc mục tiêu đầu ra..."
              ></textarea>
            </div>

            <div class="form-row">
              <div class="form-group flex-1">
                <label for="priority">Mức độ ưu tiên</label>
                <select id="priority" [(ngModel)]="priority" name="priority">
                  <option value="Low">Thấp (Low)</option>
                  <option value="Medium">Trung bình (Medium)</option>
                  <option value="High">Cao (High)</option>
                  <option value="Urgent">Khẩn cấp (Urgent)</option>
                </select>
              </div>

              <div class="form-group flex-1">
                <label for="dueDate">Hạn hoàn thành</label>
                <input
                  id="dueDate"
                  type="date"
                  [(ngModel)]="dueDate"
                  name="dueDate"
                />
              </div>
            </div>

            <div class="form-group">
              <label for="assignee">Phân công thành viên (Human hoặc Agent)</label>
              <select id="assignee" [(ngModel)]="selectedMemberId" name="assignee">
                <option [ngValue]="null">-- Chưa phân công --</option>
                @for (m of members; track m.id) {
                  <option [ngValue]="m.id">
                    {{ m.isAgent ? '🤖 [Agent] ' : '👤 [Human] ' }} {{ m.displayName }}
                  </option>
                }
              </select>
            </div>
          </div>

          <div class="modal-footer">
            <button type="button" class="btn-secondary" (click)="close.emit()">Hủy</button>
            <button type="submit" class="btn-primary" [disabled]="!title.trim()" (click)="onSubmit()">
              Tạo Tác Vụ
            </button>
          </div>
        </form>
      </div>
    </div>
  `,
  styles: [`
    .modal-backdrop {
      position: fixed;
      inset: 0;
      background: rgba(0, 0, 0, 0.4);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 9999;
    }

    .modal-card {
      background: #FFFFFF;
      width: 100%;
      max-width: 520px;
      border-radius: 12px;
      box-shadow: 0 10px 25px rgba(0, 0, 0, 0.15);
      border: 1px solid #E5E0D8;
      overflow: hidden;
    }

    .modal-header {
      padding: 16px 20px;
      border-bottom: 1px solid #EBE6DF;
      display: flex;
      justify-content: space-between;
      align-items: center;

      h3 {
        margin: 0;
        font-size: 15px;
        font-weight: 700;
        color: #111827;
      }

      .btn-close {
        background: transparent;
        border: none;
        font-size: 16px;
        cursor: pointer;
        color: #9CA3AF;
        &:hover { color: #374151; }
      }
    }

    .modal-body {
      padding: 18px 20px;

      .form-group {
        margin-bottom: 14px;

        label {
          display: block;
          font-size: 12px;
          font-weight: 600;
          color: #374151;
          margin-bottom: 5px;
        }

        input, textarea, select {
          width: 100%;
          border: 1px solid #D1D5DB;
          border-radius: 6px;
          padding: 8px 10px;
          font-size: 12px;
          box-sizing: border-box;
          &:focus {
            outline: none;
            border-color: #0061FF;
          }
        }
      }

      .form-row {
        display: flex;
        gap: 12px;

        .flex-1 { flex: 1; }
      }
    }

    .modal-footer {
      padding: 12px 20px;
      background: #F9FAFB;
      border-top: 1px solid #E5E7EB;
      display: flex;
      justify-content: flex-end;
      gap: 10px;

      button {
        padding: 8px 14px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
      }

      .btn-secondary {
        background: #FFFFFF;
        border: 1px solid #D1D5DB;
        color: #374151;
      }

      .btn-primary {
        background: #111827;
        color: #FFFFFF;
        border: none;
        &:hover { background: #000000; }
        &:disabled {
          opacity: 0.5;
          cursor: not-allowed;
        }
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TaskCreateModalComponent {
  @Input() members: ChannelMemberOption[] = [];
  @Output() close = new EventEmitter<void>();
  @Output() create = new EventEmitter<CreateTaskRequest>();

  title = '';
  description = '';
  priority: TaskPriority = 'Medium';
  dueDate: string | null = null;
  selectedMemberId: string | null = null;

  onSubmit(): void {
    if (!this.title.trim()) return;

    const selectedMember = this.members.find((m) => m.id === this.selectedMemberId);

    const request: CreateTaskRequest = {
      title: this.title.trim(),
      description: this.description.trim(),
      priority: this.priority,
      dueDate: this.dueDate ? new Date(this.dueDate).toISOString() : null,
      assigneeMemberId: this.selectedMemberId || null,
      isAgent: selectedMember?.isAgent ?? false,
    };

    this.create.emit(request);
  }
}
