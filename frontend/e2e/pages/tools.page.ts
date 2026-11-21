/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Tools Page Object Model for Playwright E2E tests
 * Manages No-Code Tool Builder interface (/tools), Tool Editor, and Sandbox Test Dialog.
 */
export class ToolsPage extends BasePage {
  readonly addToolButton: Locator;
  readonly searchInput: Locator;
  readonly toolsTable: Locator;

  // Tool Editor Modal
  readonly nameInput: Locator;
  readonly displayNameInput: Locator;
  readonly descriptionInput: Locator;
  readonly httpMethodSelect: Locator;
  readonly endpointUrlInput: Locator;
  readonly schemaTextarea: Locator;
  readonly confirmationCheckbox: Locator;
  readonly submitToolButton: Locator;
  readonly cancelToolButton: Locator;

  // Tool Test Dialog
  readonly argumentsTextarea: Locator;
  readonly executeTestButton: Locator;
  readonly testResultCard: Locator;
  readonly closeTestDialogButton: Locator;

  constructor(page: Page) {
    super(page);
    this.addToolButton = page.getByRole('button', { name: /Thêm công cụ mới/i });
    this.searchInput = page.locator('input[placeholder*="Tìm kiếm theo tên"]');
    this.toolsTable = page.locator('table');

    // Tool Editor
    this.nameInput = page.locator('input[formControlName="name"]');
    this.displayNameInput = page.locator('input[formControlName="displayName"]');
    this.descriptionInput = page.locator('textarea[formControlName="description"]');
    this.httpMethodSelect = page.locator('select[formControlName="httpMethod"]');
    this.endpointUrlInput = page.locator('input[formControlName="endpointUrl"]');
    this.schemaTextarea = page.locator('textarea[formControlName="parametersSchema"]');
    this.confirmationCheckbox = page.locator('input[formControlName="requiresConfirmation"]');
    this.submitToolButton = page.getByRole('button', {
      name: /Tạo mới Công cụ|Cập nhật Công cụ|Đăng ký công cụ|Lưu thay đổi/i,
    });
    this.cancelToolButton = page.getByRole('button', { name: /Hủy bỏ/i });

    // Tool Test Dialog
    this.argumentsTextarea = page.locator('app-tool-test-dialog textarea');
    this.executeTestButton = page.getByRole('button', { name: /Gửi yêu cầu thử nghiệm/i });
    this.testResultCard = page.locator('.rounded-lg').filter({ hasText: /Kết quả Phản hồi/i });
    this.closeTestDialogButton = page.locator('app-tool-test-dialog').getByRole('button', { name: /Đóng/i });
  }

  /**
   * Close the Test Dialog modal
   */
  async closeTestDialog(): Promise<void> {
    await this.closeTestDialogButton.click();
    await expect(this.executeTestButton).toBeHidden();
  }

  /**
   * Navigate to /tools route
   */
  async goto(): Promise<void> {
    await this.navigate('/tools');
    await this.waitForNetworkIdle();
  }

  /**
   * Register a new AI REST Tool via the Tool Editor Modal
   */
  async createTool(
    name: string,
    displayName: string,
    endpoint: string,
    method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH' = 'GET',
    schema?: string,
    requiresConfirmation: boolean = false
  ): Promise<void> {
    await this.addToolButton.click();
    await expect(this.nameInput).toBeVisible();

    await this.nameInput.fill(name);
    await this.displayNameInput.fill(displayName);
    await this.descriptionInput.fill(`Mô tả cho công cụ ${displayName}`);
    await this.httpMethodSelect.selectOption(method);
    await this.endpointUrlInput.fill(endpoint);

    if (schema) {
      await this.schemaTextarea.fill(schema);
    }

    const isChecked = await this.confirmationCheckbox.isChecked();
    if (requiresConfirmation && !isChecked) {
      await this.confirmationCheckbox.check();
    } else if (!requiresConfirmation && isChecked) {
      await this.confirmationCheckbox.uncheck();
    }

    await this.submitToolButton.click();
    await expect(this.nameInput).toBeHidden();
  }

  /**
   * Open the Test Dialog for a specific tool in the table
   */
  async openTestDialog(toolName: string): Promise<void> {
    const row = this.page.locator('tbody tr').filter({ hasText: toolName }).first();
    await expect(row).toBeVisible();
    const testButton = row.getByRole('button', { name: /Chạy thử/i });
    await testButton.click();
    await expect(this.executeTestButton).toBeVisible();
  }

  /**
   * Enter test arguments in JSON format and trigger test execution
   */
  async runTest(argsJson: string): Promise<void> {
    await this.argumentsTextarea.fill(argsJson);
    await this.executeTestButton.click();
    await expect(this.testResultCard.first()).toBeVisible({ timeout: 15000 });
  }

  /**
   * Assert that a tool is present in the tools table
   */
  async expectToolInList(toolName: string): Promise<void> {
    const row = this.page.locator('tbody tr').filter({ hasText: toolName }).first();
    await expect(row).toBeVisible();
  }
}
