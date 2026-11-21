/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Page Object đại diện cho Bảng Kanban Tác Vụ (Sprint 2 - Module Tasks).
 */
export class KanbanPage extends BasePage {
  readonly kanbanContainer: Locator;
  readonly createTaskButton: Locator;
  readonly todoColumn: Locator;
  readonly inProgressColumn: Locator;
  readonly inReviewColumn: Locator;
  readonly doneColumn: Locator;
  readonly taskCards: Locator;

  // Modal tạo tác vụ
  readonly createModal: Locator;
  readonly taskTitleInput: Locator;
  readonly taskDescTextarea: Locator;
  readonly taskPrioritySelect: Locator;
  readonly taskAssigneeSelect: Locator;
  readonly submitTaskButton: Locator;

  // Modal Review Gate
  readonly reviewModal: Locator;
  readonly reviewFeedbackTextarea: Locator;
  readonly approveButton: Locator;
  readonly requestRevisionButton: Locator;
  readonly rejectButton: Locator;

  constructor(page: Page) {
    super(page);
    this.kanbanContainer = page.locator('.kanban-board-container');
    this.createTaskButton = page.locator('.btn-create-task');
    this.todoColumn = page.locator('.kanban-column.column-todo');
    this.inProgressColumn = page.locator('.kanban-column.column-inprogress');
    this.inReviewColumn = page.locator('.kanban-column.column-inreview');
    this.doneColumn = page.locator('.kanban-column.column-done');
    this.taskCards = page.locator('.task-card');

    this.createModal = page.locator('.modal-backdrop').filter({ hasText: 'Tạo Tác Vụ Mới' });
    this.taskTitleInput = this.createModal.locator('#title, input[placeholder*="tên công việc"]');
    this.taskDescTextarea = this.createModal.locator('#description, textarea[placeholder*="yêu cầu"]');
    this.taskPrioritySelect = this.createModal.locator('.modal-body select').first();
    this.taskAssigneeSelect = this.createModal.locator('.modal-body select').nth(1);
    this.submitTaskButton = this.createModal.locator('button[type="submit"], .modal-footer .btn-primary');

    this.reviewModal = page.locator('.modal-backdrop').filter({ hasText: 'Cổng Nghiệm Thu Tác Vụ' });
    this.reviewFeedbackTextarea = page.locator('.review-modal textarea');
    this.approveButton = page.locator('.btn-action-approve');
    this.requestRevisionButton = page.locator('.btn-action-revision');
    this.rejectButton = page.locator('.btn-action-reject');
  }

  async verifyKanbanVisible() {
    await expect(this.kanbanContainer).toBeVisible();
    await expect(this.todoColumn).toBeVisible();
    await expect(this.inProgressColumn).toBeVisible();
    await expect(this.inReviewColumn).toBeVisible();
    await expect(this.doneColumn).toBeVisible();
  }

  async openCreateTaskModal() {
    await this.createTaskButton.click();
    await expect(this.createModal).toBeVisible();
  }

  async fillAndSubmitTask(title: string, description: string) {
    await this.taskTitleInput.fill(title);
    await this.taskDescTextarea.fill(description);
    await expect(this.submitTaskButton).toBeEnabled();
    await this.submitTaskButton.click();
    await expect(this.createModal).toBeHidden({ timeout: 10000 });
  }

  async expectTaskInColumn(columnLocator: Locator, taskTitle: string) {
    const card = columnLocator.locator('.task-card').filter({ hasText: taskTitle });
    await expect(card).toBeVisible();
  }
}
