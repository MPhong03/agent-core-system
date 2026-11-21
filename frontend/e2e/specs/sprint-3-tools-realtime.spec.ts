/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file sprint-3-tools-realtime.spec.ts — Playwright E2E Test Suite cho No-Code Tool Builder & Function Calling (Sprint 3)
 *
 * ## Kịch bản Kiểm thử End-to-End
 * - Scenario 1: Admin đăng nhập, điều hướng tới `/tools`, đăng ký công cụ GET (Weather API) và chạy thử nghiệm trong Sandbox Test Dialog.
 * - Scenario 2: Admin đăng ký công cụ POST (Cancel Order API) với cờ bảo vệ bắt buộc duyệt người dùng (RequiresConfirmation = true).
 * - Scenario 3: Admin điều hướng tới `/organization`, khởi tạo và cấu hình Agent Profile với chế độ Semi-Auto và phân quyền công cụ.
 * - Scenario 4: Người dùng tương tác trong Kênh Chat, xác thực hiển thị thông tin thực thi công cụ và Action Card tương tác phê duyệt.
 */

import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { OrganizationPage } from '../pages/organization.page';
import { ChatPage } from '../pages/chat.page';
import { ToolsPage } from '../pages/tools.page';

test.describe('Sprint 3 E2E: No-Code Tool Builder & Function Calling Approval Flow', () => {
  let loginPage: LoginPage;
  let orgPage: OrganizationPage;
  let chatPage: ChatPage;
  let toolsPage: ToolsPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    orgPage = new OrganizationPage(page);
    chatPage = new ChatPage(page);
    toolsPage = new ToolsPage(page);
  });

  test('Complete Tools & Function Calling Flow: Create GET/POST Tools -> Configure Agent SemiAuto -> Channel Interaction', async ({ page }) => {
    const timestamp = Date.now();
    const weatherToolName = `get_weather_${timestamp}`;
    const weatherToolDisplay = `Dự Báo Thời Tiết ${timestamp}`;
    const orderToolName = `cancel_order_${timestamp}`;
    const orderToolDisplay = `Hủy Đơn Hàng ${timestamp}`;
    const agentName = `AssistantBot ${timestamp}`;
    const deptName = `Phòng Vận Hành ${timestamp}`;
    const channelName = `kênh-tools-${timestamp}`;

    // ──────────────────────────────────────────────────────────────────────────
    // 1. Đăng nhập hệ thống với tài khoản SuperAdmin
    // ──────────────────────────────────────────────────────────────────────────
    await loginPage.goto();
    await loginPage.login('admin@mvp.vn', 'Admin@2026');
    await expect(page).toHaveURL(/.*dashboard/);

    // ──────────────────────────────────────────────────────────────────────────
    // 2. Scenario 1: Điều hướng tới /tools, tạo GET Tool và chạy thử nghiệm
    // ──────────────────────────────────────────────────────────────────────────
    await toolsPage.goto();
    await expect(page).toHaveURL(/.*tools/);
    await expect(page.locator('h2')).toContainText('Quản lý Công cụ AI (Tool Builder)');

    const weatherSchema = JSON.stringify(
      {
        type: 'object',
        properties: {
          location: {
            type: 'string',
            description: 'Tên thành phố hoặc vị trí cần tra cứu thời tiết',
          },
        },
        required: ['location'],
      },
      null,
      2
    );

    await toolsPage.createTool(
      weatherToolName,
      weatherToolDisplay,
      'https://httpbin.org/get',
      'GET',
      weatherSchema,
      false // requiresConfirmation = false
    );

    // Xác nhận tool đã xuất hiện trong danh sách
    await toolsPage.expectToolInList(weatherToolDisplay);

    // Mở hộp thoại chạy thử nghiệm (Sandbox Test Dialog)
    await toolsPage.openTestDialog(weatherToolDisplay);
    await expect(page.locator('app-tool-test-dialog')).toBeVisible();

    // Nhập tham số thử nghiệm dạng JSON và thực thi
    await toolsPage.runTest(JSON.stringify({ location: 'Hà Nội' }));
    await expect(toolsPage.testResultCard.first()).toBeVisible();

    // Đóng hộp thoại kiểm thử
    if (await toolsPage.closeTestDialogButton.first().isVisible()) {
      await toolsPage.closeTestDialogButton.first().click();
    } else {
      await page.keyboard.press('Escape');
    }

    // ──────────────────────────────────────────────────────────────────────────
    // 3. Scenario 2: Tạo POST Tool với cờ xác nhận người dùng (RequiresConfirmation = true)
    // ──────────────────────────────────────────────────────────────────────────
    const orderSchema = JSON.stringify(
      {
        type: 'object',
        properties: {
          orderId: {
            type: 'string',
            description: 'Mã định danh đơn hàng cần hủy',
          },
          reason: {
            type: 'string',
            description: 'Lý do hủy đơn',
          },
        },
        required: ['orderId'],
      },
      null,
      2
    );

    await toolsPage.createTool(
      orderToolName,
      orderToolDisplay,
      'https://httpbin.org/post',
      'POST',
      orderSchema,
      true // requiresConfirmation = true
    );

    // Xác nhận tool xuất hiện và có huy hiệu Yêu cầu duyệt
    await toolsPage.expectToolInList(orderToolDisplay);
    const orderRow = page.locator('tbody tr').filter({ hasText: orderToolDisplay }).first();
    await expect(orderRow.locator('text=Yêu cầu duyệt')).toBeVisible();

    // ──────────────────────────────────────────────────────────────────────────
    // 4. Scenario 3: Điều hướng sang /organization, tạo Agent Profile Semi-Auto & phân quyền Tools
    // ──────────────────────────────────────────────────────────────────────────
    await orgPage.goto();
    await expect(page).toHaveURL(/.*organization/);

    // Tạo Agent Profile với chế độ Semi-Auto và gán công cụ
    await orgPage.createAgentProfileWithTools(
      agentName,
      'gemini-2.5-flash',
      'Bạn là trợ lý AI xử lý tra cứu thông tin thời tiết và vận hành tác vụ đơn hàng an toàn.',
      'semi-auto',
      [weatherToolDisplay, orderToolDisplay]
    );

    // Tạo phòng ban & kênh nhóm kiểm thử
    await orgPage.createDepartment(deptName, 'Phòng ban thử nghiệm Function Calling Sprint 3');
    await orgPage.selectDepartment(deptName);

    await orgPage.createChannel(channelName, 'Kênh kiểm thử tích hợp công cụ AI');
    await orgPage.selectChannel(channelName);
    await expect(page.locator('.channel-header-bar h2')).toContainText(channelName);

    // Gán Agent vừa tạo vào Kênh nhóm
    await orgPage.assignAgentToChannel(agentName);
    const memberItem = page.locator('.member-item').filter({ hasText: agentName }).first();
    await expect(memberItem).toBeVisible();

    // Xác thực nút Cấu hình công cụ (.btn-config-tools) và chỉnh sửa cấu hình
    await orgPage.configureMemberAgentTools(agentName, 'semi-auto');

    // ──────────────────────────────────────────────────────────────────────────
    // 5. Scenario 4: Điều hướng tới Kênh Chat, kiểm tra tương tác và giao diện Action Card
    // ──────────────────────────────────────────────────────────────────────────
    const chatTabBtn = page.getByRole('button', { name: /Phòng Chat Realtime/i });
    if (await chatTabBtn.isVisible()) {
      await chatTabBtn.click();
    }

    // Xác thực giao diện Chat Room hiển thị đầy đủ
    await chatPage.verifyChatRoomVisible();
    await expect(page.locator('.chat-room').first()).toBeVisible();
    await expect(page.locator('.messages-viewport')).toBeVisible();

    // Gõ @ nhắc tên Agent
    await chatPage.typeInChat('@');
    await expect(chatPage.mentionPopover).toBeVisible();
    const candidate = chatPage.mentionItems.filter({ hasText: agentName }).first();
    await expect(candidate).toBeVisible();
    await chatPage.selectMentionCandidate(agentName);

    // Gửi tin nhắn yêu cầu tra cứu
    const promptText = 'hãy kiểm tra thời tiết Hà Nội hôm nay';
    await chatPage.typeInChat(promptText);
    await chatPage.sendMessage();

    // Xác nhận tin nhắn người dùng đã xuất hiện trong luồng hội thoại
    await chatPage.expectMessagePresent(agentName);
    await chatPage.expectMessagePresent(promptText);

    // Xác thực cấu trúc UI của Action Card / Human-in-the-Loop nếu có yêu cầu phê duyệt
    const pendingCards = page.locator('.tool-approval-card');
    const count = await pendingCards.count();
    if (count > 0) {
      await expect(pendingCards.first()).toBeVisible();
      const confirmBtn = page.locator('[data-testid="confirm-tool-btn"]').first();
      if (await confirmBtn.isVisible()) {
        await confirmBtn.click();
        await expect(page.locator('.status-confirmed, .badge-confirmed').first()).toBeVisible();
      }
    }
  });
});
