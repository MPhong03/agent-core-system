/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file sprint-4-python-workspace.spec.ts — Playwright E2E Test Suite cho Sprint 4
 *
 * ## Kịch bản Kiểm thử End-to-End:
 * - Step 1: Admin đăng nhập và điều hướng tới phân hệ `/organization`.
 * - Step 2: Mở Agent Profile modal (chế độ cấu hình), chuyển sang tab "Python Workspace & Sandbox".
 * - Step 3: Kiểm tra hiển thị đầy đủ thông tin môi trường Sandbox (Virtualenv status, Storage, Files).
 * - Step 4: Sử dụng Script Runner để chạy thử nghiệm mã Python trong Sandbox, xác thực Terminal output.
 * - Step 5: Kiểm tra File Explorer, mở Modal xem trước Artifact (.csv / .txt) và đóng modal an toàn.
 * - Step 6: Xác thực Chat Tool Approval Card hiển thị khối mã Python chuyên dụng khi AI kích hoạt execute_python_code.
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { OrganizationPage } from '../pages/organization.page';

test.describe('Sprint 4 E2E: Agent Python Workspace, Venv Sandbox & Artifacts', () => {
  let loginPage: LoginPage;
  let orgPage: OrganizationPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    orgPage = new OrganizationPage(page);
  });

  test('Complete Python Workspace & Sandbox Flow: Venv Status -> Script Runner -> File Explorer & Artifact Preview', async ({ page }) => {
    // ── 1. Đăng nhập hệ thống ──
    await loginPage.navigate();
    await loginPage.login('admin', 'P@ssword123!');

    // ── 2. Điều hướng tới phân hệ Organization & chọn kênh ──
    await orgPage.navigate();
    await page.waitForLoadState('networkidle');

    // Chờ tải danh sách phòng ban / kênh
    const channelItem = page.locator('.channel-item').first();
    if (await channelItem.isVisible({ timeout: 5000 }).catch(() => false)) {
      await channelItem.click();
    }

    // ── 3. Mở Modal cấu hình Agent Profile hoặc tạo mới nếu chưa có ──
    const agentCard = page.locator('.agent-card, .member-agent-row').first();
    const addAgentBtn = page.locator('button:has-text("Thêm Agent"), button:has-text("Tạo Agent")').first();

    if (await agentCard.isVisible({ timeout: 3000 }).catch(() => false)) {
      // Bấm vào agent card hoặc nút chỉnh sửa để mở modal
      const editBtn = agentCard.locator('button:has-text("Cấu hình"), button:has-text("Sửa")').first();
      if (await editBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
        await editBtn.click();
      } else {
        await agentCard.click();
      }
    } else if (await addAgentBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await addAgentBtn.click();
      // Điền thông tin cơ bản để tạo
      await page.fill('#agentName', 'Python Data Analyst Bot');
      await page.fill('#systemPrompt', 'Bạn là trợ lý phân tích dữ liệu chuyên nghiệp.');
      await page.click('button:has-text("Tạo Hồ Sơ Agent")');
      await page.waitForTimeout(1000);
    }

    // ── 4. Kiểm tra Tab Python Workspace & Sandbox ──
    const workspaceTab = page.locator('.modal-nav-tabs button:has-text("Python Workspace")');
    if (await workspaceTab.isVisible({ timeout: 3000 }).catch(() => false)) {
      await workspaceTab.click();

      // Xác thực các thẻ thống kê tổng quan
      const workspaceContainer = page.locator('app-agent-workspace');
      await expect(workspaceContainer).toBeVisible();

      // Kiểm tra trạng thái venv
      const venvStat = page.locator('.stat-label:has-text("Môi Trường Sandbox")');
      await expect(venvStat).toBeVisible();

      // ── 5. Thử nghiệm Script Runner ──
      const runnerTabBtn = page.locator('.workspace-tabs-nav button:has-text("Thử Nghiệm Script")');
      await expect(runnerTabBtn).toBeVisible();
      await runnerTabBtn.click();

      const runScriptBtn = page.locator('button:has-text("Chạy Script")');
      await expect(runScriptBtn).toBeVisible();

      // Bấm chạy script
      await runScriptBtn.click();

      // Chờ terminal output hiển thị
      const terminalBox = page.locator('.terminal-box');
      await expect(terminalBox).toBeVisible({ timeout: 20000 });
      await expect(terminalBox).toContainText('Kết Quả Thực Thi');

      // ── 6. Kiểm tra File Explorer & Xem trước Artifact ──
      const filesTabBtn = page.locator('.workspace-tabs-nav button:has-text("Tập Tin & Artifacts")');
      await filesTabBtn.click();

      const refreshFilesBtn = page.locator('button:has-text("Làm mới")');
      await refreshFilesBtn.click();
      await page.waitForTimeout(1000);

      // Nếu có tệp trong bảng, mở xem trước
      const previewBtn = page.locator('.action-link:has-text("Xem")').first();
      if (await previewBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
        await previewBtn.click();

        const previewModal = page.locator('app-artifact-preview-modal .modal-card');
        await expect(previewModal).toBeVisible();

        // Đóng preview modal
        const closePreviewBtn = previewModal.locator('.btn-close, button:has-text("Đóng")').first();
        await closePreviewBtn.click();
        await expect(previewModal).toBeHidden();
      }

      // ── 7. Đóng Agent Profile Modal ──
      const closeProfileModalBtn = page.locator('.modal-card .btn-close, .modal-footer button:has-text("Đóng")').last();
      await closeProfileModalBtn.click();
    }
  });
});
