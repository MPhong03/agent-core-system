import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { OrganizationPage } from '../pages/organization.page';
import { ChatPage } from '../pages/chat.page';

test.describe('Sprint 1 E2E: Realtime Chat Room & @Mention Router', () => {
  let loginPage: LoginPage;
  let orgPage: OrganizationPage;
  let chatPage: ChatPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    orgPage = new OrganizationPage(page);
    chatPage = new ChatPage(page);
  });

  test('Complete Chat Flow: Login -> Select Channel -> Verify UI -> Mention Agent -> Send Message', async ({ page }) => {
    // 1. Đăng nhập hệ thống với tài khoản SuperAdmin đã seed sẵn
    await loginPage.goto();
    await loginPage.login('admin@mvp.vn', 'Admin@2026');

    // 2. Chờ chuyển hướng sang Dashboard thành công
    await expect(page).toHaveURL(/.*dashboard/);

    // 3. Điều hướng tới module Tổ chức (Organization)
    await orgPage.goto();
    await expect(page).toHaveURL(/.*organization/);

    // 4. Tạo Agent Profile cho thử nghiệm đàm thoại
    const timestamp = Date.now();
    const agentName = `ChatBot ${timestamp}`;
    await orgPage.createAgentProfile(
      agentName,
      'gemini-2.5-flash',
      'Bạn là trợ lý AI chuyên xử lý phân tích dữ liệu và phản hồi tin nhắn trong kênh.'
    );

    // 5. Tạo Phòng ban mới
    const deptName = `Dept Chat ${timestamp}`;
    await orgPage.createDepartment(deptName, 'Phòng ban phục vụ kiểm thử Chat Realtime');

    const deptLocator = page.locator('.dept-item').filter({ hasText: deptName });
    await expect(deptLocator).toBeVisible();
    await orgPage.selectDepartment(deptName);

    // 6. Tạo Kênh nhóm thảo luận
    const channelName = `kênh-chat-${timestamp}`;
    await orgPage.createChannel(channelName, 'Kênh kiểm thử thời gian thực');

    const channelLocator = page.locator('.channel-item').filter({ hasText: channelName });
    await expect(channelLocator).toBeVisible();

    // 7. Chọn Kênh nhóm
    await orgPage.selectChannel(channelName);
    await expect(page.locator('.channel-header-bar h2')).toContainText(channelName);

    // 8. Gán Agent vừa tạo vào Kênh
    await orgPage.assignAgentToChannel(agentName);

    // Chuyển sang Tab "Phòng Chat Realtime" nếu chưa được kích hoạt
    const chatTabBtn = page.getByRole('button', { name: /Phòng Chat Realtime/i });
    if (await chatTabBtn.isVisible()) {
      await chatTabBtn.click();
    }

    // 9. Xác thực giao diện Chat Room đã hiển thị đầy đủ (.chat-room, .messages-viewport, textarea)
    await chatPage.verifyChatRoomVisible();
    await expect(page.locator('.chat-room').first()).toBeVisible();
    await expect(page.locator('.messages-viewport')).toBeVisible();
    await expect(page.locator('textarea.chat-textarea')).toBeVisible();

    // 10. Gõ ký tự '@' vào ô nhập tin nhắn và kiểm tra popover nhắc tên (.mention-popover)
    await chatPage.typeInChat('@');
    await expect(chatPage.mentionPopover).toBeVisible();

    // Kiểm tra danh sách ứng viên trong popover có chứa Agent vừa gán
    const agentCandidate = chatPage.mentionItems.filter({ hasText: agentName });
    await expect(agentCandidate).toBeVisible();

    // 11. Chọn thành viên từ popover và kiểm tra nội dung ô input được chèn `@DisplayName`
    await chatPage.selectMentionCandidate(agentName);
    await expect(chatPage.mentionPopover).toBeHidden();

    const expectedMentionPrefix = `@${agentName} `;
    await expect(chatPage.textarea).toHaveValue(new RegExp(`^${expectedMentionPrefix}`));

    // 12. Nhập tiếp nội dung câu hỏi và gửi tin nhắn
    const messageContent = 'hãy hỗ trợ kiểm tra tiến độ dự án nhé!';
    await chatPage.typeInChat(messageContent);

    const fullMessage = `${expectedMentionPrefix}${messageContent}`;
    await chatPage.sendMessage();

    // 13. Xác minh tin nhắn xuất hiện trong danh sách tin nhắn của kênh
    await chatPage.expectMessagePresent(agentName);
    await chatPage.expectMessagePresent(messageContent);
  });
});
