/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file review-gate-modal.component.ts — Modal Nghiệm thu Cổng Human-in-the-Loop (Sprint 2 - PBI-TASK-01)
 */

import { ChangeDetectionStrategy, Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TaskItem } from '../../models/task.model';

@Component({
  selector: 'app-review-gate-modal',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="modal-backdrop" (click)="close.emit()">
      <div class="modal-card" (click)="$event.stopPropagation()">
        <div class="modal-header">
          <div>
            <span class="badge-review">CỔNG NGHIỆM THU</span>
            <h3>{{ task.taskCode }}: {{ task.title }}</h3>
          </div>
          <button type="button" class="btn-close" (click)="close.emit()">✕</button>
        </div>

        <div class="modal-body">
          <p class="instruction">
            Nhân sự AI đã hoàn tất thực thi tác vụ và bàn giao sản phẩm đầu ra vào Cổng Nghiệm Thu.
            Vui lòng đánh giá kết quả để quyết định Duyệt hoàn tất hoặc Yêu cầu làm lại kèm phản hồi.
          </p>

          <div class="decision-selector">
            <label class="radio-label" [class.selected]="isApproved">
              <input type="radio" [value]="true" [(ngModel)]="isApproved" name="decision" />
              <div class="radio-content">
                <strong>✓ Phê Duyệt (Approve)</strong>
                <span>Chuyển tác vụ sang Hoàn Thành (Done).</span>
              </div>
            </label>

            <label class="radio-label" [class.selected]="!isApproved">
              <input type="radio" [value]="false" [(ngModel)]="isApproved" name="decision" />
              <div class="radio-content">
                <strong>↺ Yêu Cầu Làm Lại (Request Revision)</strong>
                <span>Trả về Đang Xử Lý. Góp ý sẽ được nạp vào Bộ nhớ riêng của Agent để học kinh nghiệm.</span>
              </div>
            </label>
          </div>

          <div class="form-group">
            <label for="feedback">Nhận xét / Góp ý phản hồi cho Agent:</label>
            <textarea
              id="feedback"
              rows="3"
              [(ngModel)]="feedback"
              placeholder="Nhập nhận xét chi tiết để Agent rút kinh nghiệm..."
            ></textarea>
          </div>
        </div>

        <div class="modal-footer">
          <button type="button" class="btn-secondary" (click)="close.emit()">Hủy</button>
          <button
            type="button"
            class="btn-primary"
            [class.btn-success]="isApproved"
            [class.btn-warning]="!isApproved"
            (click)="onSubmit()"
          >
            {{ isApproved ? 'Xác Nhận Duyệt' : 'Gửi Yêu Cầu Sửa' }}
          </button>
        </div>
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
      align-items: flex-start;

      .badge-review {
        font-size: 10px;
        font-weight: 800;
        background: #EEF2FF;
        color: #4338CA;
        padding: 2px 6px;
        border-radius: 4px;
        margin-bottom: 4px;
        display: inline-block;
      }

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

      .instruction {
        font-size: 12px;
        color: #4B5563;
        line-height: 1.5;
        margin-bottom: 16px;
      }

      .decision-selector {
        display: flex;
        flex-direction: column;
        gap: 8px;
        margin-bottom: 16px;

        .radio-label {
          display: flex;
          align-items: flex-start;
          gap: 10px;
          padding: 10px 14px;
          border: 1px solid #E5E7EB;
          border-radius: 8px;
          cursor: pointer;
          transition: all 0.15s;

          &.selected {
            border-color: #6366F1;
            background: #F5F7FF;
          }

          input {
            margin-top: 3px;
          }

          .radio-content {
            display: flex;
            flex-direction: column;
            gap: 2px;

            strong {
              font-size: 13px;
              color: #1F2937;
            }

            span {
              font-size: 11px;
              color: #6B7280;
            }
          }
        }
      }

      .form-group {
        label {
          display: block;
          font-size: 12px;
          font-weight: 600;
          color: #374151;
          margin-bottom: 6px;
        }

        textarea {
          width: 100%;
          border: 1px solid #D1D5DB;
          border-radius: 6px;
          padding: 8px 10px;
          font-size: 12px;
          box-sizing: border-box;
          &:focus {
            outline: none;
            border-color: #6366F1;
          }
        }
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

      .btn-success {
        background: #059669;
        color: #FFFFFF;
        border: none;
        &:hover { background: #047857; }
      }

      .btn-warning {
        background: #D97706;
        color: #FFFFFF;
        border: none;
        &:hover { background: #B45309; }
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewGateModalComponent {
  @Input({ required: true }) task!: TaskItem;
  @Input() initialApproval = true;
  @Output() close = new EventEmitter<void>();
  @Output() decision = new EventEmitter<{ isApproved: boolean; feedback?: string }>();

  isApproved = true;
  feedback = '';

  ngOnInit(): void {
    this.isApproved = this.initialApproval;
  }

  onSubmit(): void {
    this.decision.emit({
      isApproved: this.isApproved,
      feedback: this.feedback.trim() || undefined,
    });
  }
}
