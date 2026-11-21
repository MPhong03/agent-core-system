/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { OrganizationPage } from '../pages/organization.page';
import { KanbanPage } from '../pages/kanban.page';

test.describe('Sprint 2 E2E: Kanban Board & Human-in-the-Loop Review Gate', () => {
  let loginPage: LoginPage;
  let orgPage: OrganizationPage;
  let kanbanPage: KanbanPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    orgPage = new OrganizationPage(page);
    kanbanPage = new KanbanPage(page);
  });

  test('Complete Kanban Flow: Switch to Kanban Tab -> Create Task -> View Card in Columns', async ({ page }) => {
    // 1. Đăng nhập hệ thống với tài khoản SuperAdmin đã seed sẵn
    await loginPage.goto();
    await loginPage.login('admin@mvp.vn', 'Admin@2026');

    // 2. Chờ chuyển hướng sang Dashboard thành công
    await expect(page).toHaveURL(/.*dashboard/);

    // 3. Điều hướng tới module Tổ chức (Organization)
    await orgPage.goto();
    await expect(page).toHaveURL(/.*organization/);

    // 4. Tạo Agent Profile cho thử nghiệm tác vụ
    const timestamp = Date.now();
    const agentName = `TaskBot ${timestamp}`;
    await orgPage.createAgentProfile(
      agentName,
      'gemini-2.5-flash',
      'Bạn là trợ lý AI thực thi các tác vụ nghiên cứu thị trường và phân tích dữ liệu.'
    );

    // 5. Tạo Phòng ban mới
    const deptName = `Dept Tasks ${timestamp}`;
    await orgPage.createDepartment(deptName, 'Phòng ban phục vụ kiểm thử Kanban Board');

    const deptLocator = page.locator('.dept-item').filter({ hasText: deptName });
    await expect(deptLocator).toBeVisible();
    await orgPage.selectDepartment(deptName);

    // 6. Tạo Kênh nhóm
    const channelName = `kênh-tasks-${timestamp}`;
    await orgPage.createChannel(channelName, 'Kênh quản lý bảng tác vụ');

    const channelLocator = page.locator('.channel-item').filter({ hasText: channelName });
    await expect(channelLocator).toBeVisible();

    // 7. Chọn Kênh nhóm
    await orgPage.selectChannel(channelName);
    await expect(page.locator('.channel-header-bar h2')).toContainText(channelName);

    // 8. Gán Agent vừa tạo vào Kênh
    await orgPage.assignAgentToChannel(agentName);

    // 9. Chuyển sang Tab "Bảng Kanban"
    const kanbanTabBtn = page.getByRole('button', { name: /Bảng Kanban/i });
    await expect(kanbanTabBtn).toBeVisible();
    await kanbanTabBtn.click();

    // 10. Xác thực 4 cột Kanban hiển thị đầy đủ
    await kanbanPage.verifyKanbanVisible();
    await expect(page.locator('.kanban-board-container')).toBeVisible();

    // 11. Mở modal tạo tác vụ và tạo tác vụ mới
    const taskTitle = `Nghiên cứu thị trường AI ${timestamp}`;
    const taskDesc = 'Khảo sát và lập bảng so sánh tính năng của các nền tảng AI Agent hiện nay.';
    await kanbanPage.openCreateTaskModal();
    await kanbanPage.fillAndSubmitTask(taskTitle, taskDesc);

    // 12. Xác thực thẻ tác vụ xuất hiện trong cột "CẦN LÀM" (To Do)
    await kanbanPage.expectTaskInColumn(kanbanPage.todoColumn, taskTitle);
    const createdCard = kanbanPage.todoColumn.locator('.task-card').filter({ hasText: taskTitle });
    await expect(createdCard).toBeVisible();
    await expect(createdCard.locator('.task-title, .card-title, h4').first()).toContainText(taskTitle);
  });
});
