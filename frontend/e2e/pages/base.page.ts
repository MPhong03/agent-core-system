import { Page, Locator } from '@playwright/test';

/**
 * Base Page Object Model for Playwright E2E tests
 */
export abstract class BasePage {
  readonly page: Page;

  constructor(page: Page) {
    this.page = page;
  }

  async navigate(path: string): Promise<void> {
    await this.page.goto(path);
  }

  async waitForNetworkIdle(): Promise<void> {
    await this.page.waitForLoadState('networkidle');
  }

  async takeScreenshot(name: string): Promise<Buffer> {
    return this.page.screenshot({ path: `screenshots/${name}.png`, fullPage: true });
  }
}
