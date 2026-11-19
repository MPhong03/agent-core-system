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
}
