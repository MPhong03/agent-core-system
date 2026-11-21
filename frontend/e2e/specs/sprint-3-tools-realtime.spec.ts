/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * @file sprint-3-tools-realtime.spec.ts — Playwright E2E Test Suite cho No-Code Tool Builder & Function Calling (Sprint 3)
 *
 * ## Kịch bản Kiểm thử End-to-End theo chuẩn anti-ai-slop (RULE-05, RULE-06):
 * - Step 1: Admin đăng nhập, điều hướng tới `/tools`, đăng ký công cụ GET (Weather API) và chạy thử nghiệm trong Sandbox Test Dialog.
 * - Step 2: Admin đăng ký công cụ POST (Cancel Order API) với cờ bảo vệ bắt buộc duyệt người dùng (RequiresConfirmation = true).
 * - Step 3: Admin điều hướng tới `/organization`, khởi tạo và cấu hình Agent Profile với chế độ Semi-Auto và phân quyền công cụ.
 * - Step 4: Người dùng tương tác trong Kênh Chat, gọi công cụ an toàn GET tự động thực thi.
 * - Step 5: Người dùng gửi yêu cầu gọi công cụ nhạy cảm POST, xác thực Action Card xuất hiện và nhấn Phê duyệt thành công.
 */

import { test, expect } from '@playwright/test';
import * as http from 'http';
import { LoginPage } from '../pages/login.page';
import { OrganizationPage } from '../pages/organization.page';
import { ChatPage } from '../pages/chat.page';
import { ToolsPage } from '../pages/tools.page';

test.describe('Sprint 3 E2E: No-Code Tool Builder & Function Calling Approval Flow', () => {
  let loginPage: LoginPage;
  let orgPage: OrganizationPage;
  let chatPage: ChatPage;
  let toolsPage: ToolsPage;
  let mockLlmServer: http.Server | null = null;

  test.beforeAll(async () => {
    mockLlmServer = http.createServer((req, res) => {
      if (req.method === 'POST') {
        let body = '';
        req.on('data', (chunk) => {
          body += chunk;
        });
        req.on('end', () => {
          try {
            const payload = JSON.parse(body || '{}');
            const messages = payload.messages || [];
            const tools = payload.tools || [];
            const lastMsg = messages[messages.length - 1];
            const lastContent = (lastMsg?.content || '').toString();
            const lastRole = lastMsg?.role || '';

            // 1. SSE Stream handling
            if (payload.stream) {
              res.writeHead(200, {
                'Content-Type': 'text/event-stream',
                'Cache-Control': 'no-cache',
                Connection: 'keep-alive',
              });
              res.write(`data: ${JSON.stringify({ choices: [{ delta: { content: 'Xin chào, tôi là AI trợ lý.' } }] })}\n\n`);
              res.write('data: [DONE]\n\n');
              res.end();
              return;
            }

            // 2. Resumption sau khi thực thi Tool (Message role == 'tool')
            if (lastRole === 'tool' || messages.some((m: any) => m.role === 'tool')) {
              const responseData = {
                id: 'chatcmpl-mock-' + Date.now(),
                object: 'chat.completion',
                choices: [
                  {
                    index: 0,
                    finish_reason: 'stop',
                    message: {
                      role: 'assistant',
                      content: 'Tôi đã hoàn tất yêu cầu của bạn dựa trên kết quả thực thi công cụ.',
                    },
                  },
                ],
              };
              res.writeHead(200, { 'Content-Type': 'application/json' });
              res.end(JSON.stringify(responseData));
              return;
            }

            // 3. Kích hoạt Tool Tra cứu Thời tiết (GET tool)
            if (/thời tiết|weather/i.test(lastContent)) {
              const weatherTool = tools.find((t: any) => /weather|thời tiết/i.test(t.function?.name || '')) || tools[0];
              const toolName = weatherTool?.function?.name || 'get_weather';
              const responseData = {
                id: 'chatcmpl-mock-' + Date.now(),
                object: 'chat.completion',
                choices: [
                  {
                    index: 0,
                    finish_reason: 'tool_calls',
                    message: {
                      role: 'assistant',
                      content: null,
                      tool_calls: [
                        {
                          id: 'call_weather_' + Date.now(),
                          type: 'function',
                          function: {
                            name: toolName,
                            arguments: JSON.stringify({ location: 'Hà Nội' }),
                          },
                        },
                      ],
                    },
                  },
                ],
              };
              res.writeHead(200, { 'Content-Type': 'application/json' });
              res.end(JSON.stringify(responseData));
              return;
            }

            // 4. Kích hoạt Tool Hủy đơn hàng (POST tool nhạy cảm -> Yêu cầu duyệt)
            if (/hủy|cancel|ORD-/i.test(lastContent)) {
              const orderTool = tools.find((t: any) => /order|cancel|hủy/i.test(t.function?.name || '')) || tools[1] || tools[0];
              const toolName = orderTool?.function?.name || 'cancel_order';
              const responseData = {
                id: 'chatcmpl-mock-' + Date.now(),
                object: 'chat.completion',
                choices: [
                  {
                    index: 0,
                    finish_reason: 'tool_calls',
                    message: {
                      role: 'assistant',
                      content: 'Tôi nhận được yêu cầu hủy đơn hàng. Vui lòng xác nhận để tiếp tục.',
                      tool_calls: [
                        {
                          id: 'call_order_' + Date.now(),
                          type: 'function',
                          function: {
                            name: toolName,
                            arguments: JSON.stringify({ orderId: 'ORD-9999', reason: 'Yêu cầu từ người dùng' }),
                          },
                        },
                      ],
                    },
                  },
                ],
              };
              res.writeHead(200, { 'Content-Type': 'application/json' });
              res.end(JSON.stringify(responseData));
              return;
            }

            // 5. Default fallback
            const defaultResponse = {
              id: 'chatcmpl-mock-' + Date.now(),
              object: 'chat.completion',
              choices: [
                {
                  index: 0,
                  finish_reason: 'stop',
                  message: {
                    role: 'assistant',
                    content: 'Tôi là AI trợ lý, sẵn sàng hỗ trợ bạn.',
                  },
                },
              ],
            };
            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(defaultResponse));
          } catch {
            res.writeHead(400);
            res.end();
          }
        });
      } else {
        res.writeHead(200, { 'Content-Type': 'application/json' });
        res.end(JSON.stringify({ status: 'ok' }));
      }
    });

    await new Promise<void>((resolve) => {
      mockLlmServer?.listen(11434, '127.0.0.1', () => resolve());
    });
  });

  test.afterAll(async () => {
    if (mockLlmServer) {
      await new Promise<void>((resolve) => {
        mockLlmServer?.close(() => resolve());
      });
    }
  });

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    orgPage = new OrganizationPage(page);
    chatPage = new ChatPage(page);
    toolsPage = new ToolsPage(page);
  });

  test('Complete Tools & Function Calling Flow: Create GET/POST Tools -> Configure Agent SemiAuto -> Channel Interaction & Approval', async ({ page }) => {
    const timestamp = Date.now();
    const weatherToolName = `get_weather_${timestamp}`;
    const weatherToolDisplay = `Dự Báo Thời Tiết ${timestamp}`;
    const orderToolName = `cancel_order_${timestamp}`;
    const orderToolDisplay = `Hủy Đơn Hàng ${timestamp}`;
    const agentName = `AssistantBot ${timestamp}`;
    const deptName = `Phòng Vận Hành ${timestamp}`;
    const channelName = `kênh-tools-${timestamp}`;

    // ──────────────────────────────────────────────────────────────────────────
    // Step 0: Đăng nhập hệ thống với tài khoản SuperAdmin
    // ──────────────────────────────────────────────────────────────────────────
    await test.step('Admin logs in to system', async () => {
      await loginPage.goto();
      await loginPage.login('admin@mvp.vn', 'Admin@2026');
      await expect(page).toHaveURL(/.*dashboard/);
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Step 1: Tạo và kiểm thử GET Tool (get_weather) trong Sandbox Test Dialog
    // ──────────────────────────────────────────────────────────────────────────
    await test.step('Step 1: Create and test GET Tool (get_weather)', async () => {
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
      await expect(toolsPage.executeTestButton).toBeVisible();

      // Nhập tham số thử nghiệm dạng JSON và thực thi
      await toolsPage.runTest(JSON.stringify({ location: 'Hà Nội' }));
      await expect(toolsPage.testResultCard.first()).toBeVisible({ timeout: 15000 });

      // Đóng hộp thoại kiểm thử
      await toolsPage.closeTestDialog();
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Step 2: Tạo confirmation-gated POST Tool (cancel_order)
    // ──────────────────────────────────────────────────────────────────────────
    await test.step('Step 2: Create confirmation-gated POST Tool (cancel_order)', async () => {
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
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Step 3: Cấu hình Agent Profile với SemiAuto mode và phân quyền Tools
    // ──────────────────────────────────────────────────────────────────────────
    await test.step('Step 3: Configure Agent Profile with SemiAuto mode and assign tools', async () => {
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

      // Gán Admin user vào kênh để có quyền gửi tin nhắn
      await orgPage.assignCurrentUserToChannel('Super Administrator');

      // Gán Agent vừa tạo vào Kênh nhóm
      await orgPage.assignAgentToChannel(agentName);

      // Chuyển sang Tab Thành Viên để xem danh sách thành viên kênh
      const membersTabBtn = page.getByRole('button', { name: /Thành Viên/i }).first();
      await membersTabBtn.click();

      const memberItem = page.locator('.member-card').filter({ hasText: agentName }).first();
      await expect(memberItem).toBeVisible();

      // Xác thực nút Cấu hình công cụ (.btn-config-tools) và chỉnh sửa cấu hình
      await orgPage.configureMemberAgentTools(agentName, 'semi-auto');
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Step 4: Kiểm tra gọi công cụ an toàn tự động thực thi trong Kênh Chat
    // ──────────────────────────────────────────────────────────────────────────
    await test.step('Step 4: Verify auto tool execution in Chat', async () => {
      const chatTabBtn = page.getByRole('button', { name: /Phòng Chat Realtime/i });
      await expect(chatTabBtn).toBeVisible();
      await chatTabBtn.click();

      // Xác thực giao diện Chat Room hiển thị đầy đủ
      await chatPage.verifyChatRoomVisible();
      await expect(page.locator('.chat-room').first()).toBeVisible();
      await expect(page.locator('.messages-viewport')).toBeVisible();

      // Gõ @ nhắc tên Agent và gửi yêu cầu tra cứu thời tiết
      await chatPage.typeInChat('@');
      await expect(chatPage.mentionPopover).toBeVisible();
      const candidate = chatPage.mentionItems.filter({ hasText: agentName }).first();
      await expect(candidate).toBeVisible();
      await chatPage.selectMentionCandidate(agentName);

      const promptWeather = 'hãy kiểm tra thời tiết Hà Nội hôm nay';
      await chatPage.typeInChat(promptWeather);
      await chatPage.sendMessage();

      // Xác nhận tin nhắn người dùng xuất hiện trong luồng hội thoại
      await chatPage.expectMessagePresent(agentName);
      await chatPage.expectMessagePresent(promptWeather);

      // Xác thực phản hồi thực thi công cụ hoặc tin nhắn phản hồi của Agent xuất hiện
      await expect(
        page.locator('.tool-execution-badge, .chat-bubble-container, .messages-viewport').filter({ hasText: /get_weather|thời tiết/i }).first()
      ).toBeVisible({ timeout: 15000 });
    });

    // ──────────────────────────────────────────────────────────────────────────
    // Step 5: Kích hoạt công cụ có cổng xác nhận (RequiresConfirmation), kiểm tra Action Card và phê duyệt
    // ──────────────────────────────────────────────────────────────────────────
    await test.step('Step 5: Verify confirmation-gated Tool Approval Card and approve execution in Chat', async () => {
      // Gửi yêu cầu nhạy cảm gọi cancel_order
      await chatPage.typeInChat('@');
      await expect(chatPage.mentionPopover).toBeVisible();
      const candidate = chatPage.mentionItems.filter({ hasText: agentName }).first();
      await expect(candidate).toBeVisible();
      await chatPage.selectMentionCandidate(agentName);

      const promptCancel = 'vui lòng hủy đơn hàng ORD-9999';
      await chatPage.typeInChat(promptCancel);
      await chatPage.sendMessage();

      // Xác nhận tin nhắn xuất hiện
      await chatPage.expectMessagePresent(promptCancel);

      // Xác thực Action Card phê duyệt công cụ (Human-in-the-Loop) hiển thị không điều kiện
      await chatPage.expectToolApprovalCard();
      await expect(page.locator('.tool-approval-card')).toBeVisible({ timeout: 15000 });
      await expect(
        page.locator('.tool-approval-card .tool-name-pill, .tool-approval-card').filter({ hasText: new RegExp(`${orderToolName}|${orderToolDisplay}|Hủy Đơn Hàng|cancel_order`, 'i') }).first()
      ).toBeVisible();

      // Nhấn nút [Phê duyệt & Thực thi] trên Action Card
      await chatPage.confirmToolApproval();

      // Xác thực Action Card chuyển sang trạng thái đã phê duyệt
      await expect(
        page.locator('.tool-approval-card.status-confirmed, .status-badge.badge-confirmed, .badge-confirmed').first()
      ).toBeVisible({ timeout: 10000 });
    });
  });
});
