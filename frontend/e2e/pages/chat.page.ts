/**
 * Copyright (c) Phong Minh Dang <dangminhphong912@gmail.com>. All rights reserved.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Chat Room Page Object Model for Playwright E2E tests
 */
export class ChatPage extends BasePage {
  readonly chatRoom: Locator;
  readonly messagesViewport: Locator;
  readonly textarea: Locator;
  readonly sendButton: Locator;
  readonly mentionPopover: Locator;
  readonly mentionItems: Locator;
  readonly chatBubbles: Locator;
  readonly connectionPill: Locator;
  readonly toolApprovalCard: Locator;
  readonly confirmToolButton: Locator;
  readonly rejectToolButton: Locator;
  readonly toolExecutionBadge: Locator;

  constructor(page: Page) {
    super(page);
    this.chatRoom = page.locator('.chat-room');
    this.messagesViewport = page.locator('.messages-viewport');
    this.textarea = page.locator('textarea.chat-textarea');
    this.sendButton = page.locator('button.btn-send');
    this.mentionPopover = page.locator('.mention-popover');
    this.mentionItems = page.locator('.mention-popover .member-item');
    this.chatBubbles = page.locator('.chat-bubble-container');
    this.connectionPill = page.locator('.connection-pill');
    this.toolApprovalCard = page.locator('.tool-approval-card');
    this.confirmToolButton = page.locator('[data-testid="confirm-tool-btn"]');
    this.rejectToolButton = page.locator('[data-testid="reject-tool-btn"]');
    this.toolExecutionBadge = page.locator('.tool-execution-badge');
  }

  /**
   * Verify core Chat Room components are rendered
   */
  async verifyChatRoomVisible(): Promise<void> {
    await expect(this.chatRoom.first()).toBeVisible();
    await expect(this.messagesViewport).toBeVisible();
    await expect(this.textarea).toBeVisible();
  }

  /**
   * Type text into chat textarea character by character to trigger input & mention events
   */
  async typeInChat(text: string): Promise<void> {
    await this.textarea.focus();
    await this.textarea.pressSequentially(text, { delay: 50 });
  }

  /**
   * Verify mention popover is open and select a candidate by name
   */
  async selectMentionCandidate(displayName: string): Promise<void> {
    await expect(this.mentionPopover).toBeVisible();
    const candidate = this.mentionItems.filter({ hasText: displayName }).first();
    await expect(candidate).toBeVisible();
    await candidate.click();
  }

  /**
   * Click send button
   */
  async sendMessage(): Promise<void> {
    await this.sendButton.click();
  }

  /**
   * Expect a message bubble with text snippet to be present in chat
   */
  async expectMessagePresent(contentSnippet: string): Promise<void> {
    const bubble = this.chatBubbles.filter({ hasText: contentSnippet }).first();
    await expect(bubble).toBeVisible();
  }

  /**
   * Expect a tool approval action card (Human-in-the-Loop) to be rendered
   */
  async expectToolApprovalCard(toolName?: string): Promise<void> {
    if (toolName) {
      await expect(this.page.locator('.tool-approval-card').filter({ hasText: toolName }).first()).toBeVisible({ timeout: 15000 });
    } else {
      await expect(this.page.locator('.tool-approval-card').first()).toBeVisible({ timeout: 15000 });
    }
  }

  /**
   * Click the approval button on a tool action card
   */
  async confirmToolApproval(): Promise<void> {
    const confirmBtn = this.page.locator('[data-testid="confirm-tool-btn"]').first();
    await expect(confirmBtn).toBeVisible();
    await confirmBtn.click();
  }

  /**
   * Click the reject button on a tool action card
   */
  async rejectToolApproval(): Promise<void> {
    const rejectBtn = this.page.locator('[data-testid="reject-tool-btn"]').first();
    await expect(rejectBtn).toBeVisible();
    await rejectBtn.click();
  }

  /**
   * Expect tool execution status badge to be visible
   */
  async expectToolExecutionBadge(toolName?: string): Promise<void> {
    if (toolName) {
      await expect(this.page.locator('.tool-execution-badge').filter({ hasText: toolName }).first()).toBeVisible();
    } else {
      await expect(this.page.locator('.tool-execution-badge').first()).toBeVisible();
    }
  }
}
