/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file kanban-board.component.ts — Bảng Kanban Kéo & Thả 4 Cột và Cổng Nghiệm Thu (Sprint 2 - PBI-TASK-01)
 */

import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnChanges,
  OnInit,
  SimpleChanges,
  inject,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { CreateTaskRequest, TaskDetail, TaskItem, TaskItemStatus } from '../../models/task.model';
import { TasksStore } from '../../store/tasks.store';
import { TaskCardComponent } from '../task-card/task-card.component';
import { TaskCreateModalComponent, ChannelMemberOption } from '../task-create-modal/task-create-modal.component';
import { TaskDetailModalComponent } from '../task-detail-modal/task-detail-modal.component';
import { ReviewGateModalComponent } from '../review-gate-modal/review-gate-modal.component';

@Component({
  selector: 'app-kanban-board',
  standalone: true,
  imports: [
    CommonModule,
    DragDropModule,
    TaskCardComponent,
    TaskCreateModalComponent,
    TaskDetailModalComponent,
    ReviewGateModalComponent,
  ],
  template: `
    <div class="kanban-wrapper">
      <!-- Toolbar Header -->
      <div class="kanban-toolbar">
        <div class="toolbar-stats">
          <span class="stat-pill">Tổng: <strong>{{ store.totalTasksCount() }}</strong></span>
          <span class="stat-pill in-review">Cần nghiệm thu: <strong>{{ store.inReviewCount() }}</strong></span>
        </div>

        <button type="button" class="btn-create-task" (click)="isCreateModalOpen.set(true)">
          + Tạo Tác Vụ Mới
        </button>
      </div>

      <!-- 4 Kanban Columns with Drag & Drop -->
      <div class="kanban-columns" cdkDropListGroup>

        <!-- Column 1: TO DO -->
        <div class="kanban-column column-todo">
          <div class="col-header">
            <div class="col-title">
              <span class="dot-indicator dot-todo"></span>
              <h3>CẦN LÀM</h3>
            </div>
            <span class="count-badge">{{ store.todoTasks().length }}</span>
          </div>

          <div
            id="todo-list"
            cdkDropList
            [cdkDropListData]="store.todoTasks()"
            (cdkDropListDropped)="onDrop($event, 'ToDo')"
            class="cards-drop-zone custom-scrollbar"
          >
            @for (task of store.todoTasks(); track task.id) {
              <div cdkDrag [cdkDragData]="task">
                <app-task-card
                  [task]="task"
                  (viewDetails)="onViewDetails($event)"
                ></app-task-card>
              </div>
            } @empty {
              <div class="empty-column-placeholder">Chưa có tác vụ</div>
            }
          </div>
        </div>

        <!-- Column 2: IN PROGRESS -->
        <div class="kanban-column column-inprogress">
          <div class="col-header">
            <div class="col-title">
              <span class="dot-indicator dot-inprogress"></span>
              <h3>ĐANG XỬ LÝ</h3>
            </div>
            <span class="count-badge">{{ store.inProgressTasks().length }}</span>
          </div>

          <div
            id="inprogress-list"
            cdkDropList
            [cdkDropListData]="store.inProgressTasks()"
            (cdkDropListDropped)="onDrop($event, 'InProgress')"
            class="cards-drop-zone custom-scrollbar"
          >
            @for (task of store.inProgressTasks(); track task.id) {
              <div cdkDrag [cdkDragData]="task">
                <app-task-card
                  [task]="task"
                  (viewDetails)="onViewDetails($event)"
                ></app-task-card>
              </div>
            } @empty {
              <div class="empty-column-placeholder">Không có việc đang chạy</div>
            }
          </div>
        </div>

        <!-- Column 3: IN REVIEW (REVIEW GATE) -->
        <div class="kanban-column column-inreview">
          <div class="col-header">
            <div class="col-title">
              <span class="dot-indicator dot-inreview"></span>
              <h3>CHỜ NGHIỆM THU</h3>
            </div>
            <span class="count-badge highlight">{{ store.inReviewTasks().length }}</span>
          </div>

          <div
            id="inreview-list"
            cdkDropList
            [cdkDropListData]="store.inReviewTasks()"
            (cdkDropListDropped)="onDrop($event, 'InReview')"
            class="cards-drop-zone custom-scrollbar"
          >
            @for (task of store.inReviewTasks(); track task.id) {
              <div cdkDrag [cdkDragData]="task">
                <app-task-card
                  [task]="task"
                  (viewDetails)="onViewDetails($event)"
                  (reviewGate)="onReviewGateAction($event)"
                ></app-task-card>
              </div>
            } @empty {
              <div class="empty-column-placeholder">Chưa có sản phẩm nộp</div>
            }
          </div>
        </div>

        <!-- Column 4: DONE -->
        <div class="kanban-column column-done">
          <div class="col-header">
            <div class="col-title">
              <span class="dot-indicator dot-done"></span>
              <h3>HOÀN THÀNH</h3>
            </div>
            <span class="count-badge">{{ store.doneTasks().length }}</span>
          </div>

          <div
            id="done-list"
            cdkDropList
            [cdkDropListData]="store.doneTasks()"
            (cdkDropListDropped)="onDrop($event, 'Done')"
            class="cards-drop-zone custom-scrollbar"
          >
            @for (task of store.doneTasks(); track task.id) {
              <div cdkDrag [cdkDragData]="task">
                <app-task-card
                  [task]="task"
                  (viewDetails)="onViewDetails($event)"
                ></app-task-card>
              </div>
            } @empty {
              <div class="empty-column-placeholder">Chưa có tác vụ hoàn tất</div>
            }
          </div>
        </div>

      </div>

      <!-- Modals -->
      @if (isCreateModalOpen()) {
        <app-task-create-modal
          [members]="members"
          (close)="isCreateModalOpen.set(false)"
          (create)="onCreateTaskSubmit($event)"
        ></app-task-create-modal>
      }

      @if (reviewGateTask()) {
        <app-review-gate-modal
          [task]="reviewGateTask()!"
          [initialApproval]="reviewGateInitialApproval"
          (close)="reviewGateTask.set(null)"
          (decision)="onReviewDecisionSubmit($event)"
        ></app-review-gate-modal>
      }

      @if (store.selectedTask()) {
        <app-task-detail-modal
          [task]="store.selectedTask()!"
          (close)="store.selectedTask.set(null)"
        ></app-task-detail-modal>
      }
    </div>
  `,
  styles: [`
    .kanban-wrapper {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: #F7F5F2;
      padding: 16px;
      box-sizing: border-box;
      overflow: hidden;
    }

    .kanban-toolbar {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;

      .toolbar-stats {
        display: flex;
        gap: 8px;

        .stat-pill {
          font-size: 11px;
          background: #EBE7DF;
          color: #4B5563;
          padding: 4px 10px;
          border-radius: 6px;

          &.in-review {
            background: #EEF2FF;
            color: #4338CA;
          }
        }
      }

      .btn-create-task {
        background: #111827;
        color: #FFFFFF;
        border: none;
        padding: 6px 14px;
        border-radius: 6px;
        font-size: 12px;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.15s;

        &:hover {
          background: #000000;
        }
      }
    }

    .kanban-columns {
      display: grid;
      grid-template-columns: repeat(4, 1fr);
      gap: 14px;
      flex: 1;
      min-height: 0;
    }

    .kanban-column {
      background: #EFECE6;
      border: 1px solid #E1DBD1;
      border-radius: 10px;
      padding: 12px;
      display: flex;
      flex-direction: column;
      min-height: 0;

      &.column-inreview {
        background: #E9E6F7;
        border-color: #C7D2FE;
      }

      .col-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 12px;
        padding-bottom: 8px;
        border-bottom: 1px solid rgba(0, 0, 0, 0.06);

        .col-title {
          display: flex;
          align-items: center;
          gap: 6px;

          .dot-indicator {
            width: 8px;
            height: 8px;
            border-radius: 50%;

            &.dot-todo { background: #9CA3AF; }
            &.dot-inprogress { background: #F59E0B; }
            &.dot-inreview { background: #6366F1; }
            &.dot-done { background: #10B981; }
          }

          h3 {
            margin: 0;
            font-size: 11px;
            font-weight: 800;
            letter-spacing: 0.05em;
            color: #374151;
          }
        }

        .count-badge {
          font-size: 10px;
          font-weight: 700;
          background: #FFFFFF;
          color: #4B5563;
          padding: 2px 6px;
          border-radius: 10px;
          border: 1px solid rgba(0, 0, 0, 0.08);

          &.highlight {
            background: #6366F1;
            color: #FFFFFF;
            border: none;
          }
        }
      }

      .cards-drop-zone {
        flex: 1;
        overflow-y: auto;
        padding-right: 4px;
        min-height: 100px;
      }

      .empty-column-placeholder {
        text-align: center;
        padding: 30px 10px;
        font-size: 11px;
        color: #9CA3AF;
        font-style: italic;
      }
    }

    .cdk-drag-preview {
      box-shadow: 0 5px 15px rgba(0, 0, 0, 0.15);
      border-radius: 8px;
      opacity: 0.9;
    }

    .cdk-drag-placeholder {
      opacity: 0.3;
    }

    .cdk-drag-animating {
      transition: transform 250ms cubic-bezier(0, 0, 0.2, 1);
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class KanbanBoardComponent implements OnInit, OnChanges {
  @Input({ required: true }) channelId!: string;
  @Input() members: ChannelMemberOption[] = [];

  readonly store = inject(TasksStore);

  readonly isCreateModalOpen = signal<boolean>(false);
  readonly reviewGateTask = signal<TaskItem | null>(null);
  reviewGateInitialApproval = true;

  ngOnInit(): void {
    if (this.channelId) {
      void this.store.initChannel(this.channelId);
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['channelId'] && !changes['channelId'].firstChange) {
      void this.store.initChannel(this.channelId);
    }
  }

  onDrop(event: CdkDragDrop<TaskItem[]>, targetStatus: TaskItemStatus): void {
    const task = event.item.data as TaskItem;
    if (!task || task.status === targetStatus) {
      return;
    }

    if (task.status === 'InReview' && targetStatus === 'Done') {
      // Review Gate Approve
      this.reviewGateInitialApproval = true;
      this.reviewGateTask.set(task);
      return;
    }

    void this.store.moveTaskStatus(task.id, targetStatus);
  }

  onViewDetails(task: TaskItem): void {
    this.store.loadTaskDetail(task.id);
  }

  onReviewGateAction(event: { task: TaskItem; isApproved: boolean }): void {
    this.reviewGateInitialApproval = event.isApproved;
    this.reviewGateTask.set(event.task);
  }

  async onCreateTaskSubmit(request: CreateTaskRequest): Promise<void> {
    try {
      await this.store.createTask(this.channelId, request);
      this.isCreateModalOpen.set(false);
    } catch {
      // Error handled by store
    }
  }

  async onReviewDecisionSubmit(event: { isApproved: boolean; feedback?: string }): Promise<void> {
    const task = this.reviewGateTask();
    if (!task) return;

    try {
      await this.store.reviewTask(task.id, {
        isApproved: event.isApproved,
        feedback: event.feedback,
      });
      this.reviewGateTask.set(null);
    } catch {
      // Error handled by store
    }
  }
}
