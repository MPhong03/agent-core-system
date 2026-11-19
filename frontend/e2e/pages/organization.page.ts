import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

/**
 * Organization Page Object Model
 */
export class OrganizationPage extends BasePage {
  readonly addDeptButton: Locator;
  readonly addAgentButton: Locator;

  // Department Modal
  readonly deptNameInput: Locator;
  readonly deptDescInput: Locator;
  readonly submitDeptButton: Locator;

  // Channel Modal
  readonly channelNameInput: Locator;
  readonly channelDescInput: Locator;
  readonly submitChannelButton: Locator;

  // Agent Modal
  readonly agentNameInput: Locator;
  readonly modelNameInput: Locator;
  readonly systemPromptInput: Locator;
  readonly submitAgentButton: Locator;

  constructor(page: Page) {
    super(page);
    this.addDeptButton = page.getByRole('button', { name: 'Thêm Phòng Ban' }).first();
    this.addAgentButton = page.getByRole('button', { name: 'Quản Lý Hồ Sơ Agent' }).first();

    // Modals
    this.deptNameInput = page.locator('#deptName');
    this.deptDescInput = page.locator('#deptDesc');
    this.submitDeptButton = page.getByRole('button', { name: 'Tạo Phòng Ban', exact: true });

    this.channelNameInput = page.locator('#channelName');
    this.channelDescInput = page.locator('#channelDesc');
    this.submitChannelButton = page.getByRole('button', { name: 'Tạo Kênh Nhóm', exact: true });

    this.agentNameInput = page.locator('#agentName');
    this.modelNameInput = page.locator('#modelName');
    this.systemPromptInput = page.locator('#systemPrompt');
    this.submitAgentButton = page.getByRole('button', { name: /Tạo Hồ Sơ Agent|Lưu Hồ Sơ Agent/ });
  }

  async goto(): Promise<void> {
    await this.navigate('/organization');
    await this.waitForNetworkIdle();
  }

  async createDepartment(name: string, desc?: string): Promise<void> {
    await this.addDeptButton.click();
    await expect(this.deptNameInput).toBeVisible();
    await this.deptNameInput.fill(name);
    if (desc) {
      await this.deptDescInput.fill(desc);
    }
    await this.submitDeptButton.click();
    await expect(this.deptNameInput).toBeHidden();
  }

  async selectDepartment(name: string): Promise<void> {
    const deptItem = this.page.locator('.dept-item').filter({ hasText: name });
    await deptItem.click();
  }

  async createChannel(name: string, desc?: string): Promise<void> {
    const addChannelBtn = this.page.getByRole('button', { name: /Thêm Kênh|Tạo kênh/i }).first();
    await addChannelBtn.click();
    await expect(this.channelNameInput).toBeVisible();
    await this.channelNameInput.fill(name);
    if (desc) {
      await this.channelDescInput.fill(desc);
    }
    await this.submitChannelButton.click();
    await expect(this.channelNameInput).toBeHidden();
  }

  async selectChannel(name: string): Promise<void> {
    const channelItem = this.page.locator('.channel-item').filter({ hasText: name }).first();
    await channelItem.click();
  }

  async createAgentProfile(name: string, modelName: string, prompt: string): Promise<void> {
    await this.addAgentButton.click();
    await expect(this.agentNameInput).toBeVisible();
    await this.agentNameInput.fill(name);
    await this.modelNameInput.fill(modelName);
    await this.systemPromptInput.fill(prompt);
    await this.submitAgentButton.click();
    await expect(this.agentNameInput).toBeHidden();
  }

  async assignAgentToChannel(agentName: string): Promise<void> {
    const addMemberBtn = this.page.getByRole('button', { name: /Thêm Thành Viên/i }).first();
    await addMemberBtn.click();
    await expect(this.page.locator('.member-type-toggle')).toBeVisible();

    // Select Agent type toggle
    const agentToggle = this.page.locator('.member-type-toggle button').filter({ hasText: 'Agent AI' });
    await agentToggle.click();

    // Select Agent from dropdown
    const select = this.page.locator('form.modal-body select.form-control');
    await select.waitFor({ state: 'visible' });
    const option = select.locator('option').filter({ hasText: agentName });
    const optionVal = await option.getAttribute('value');
    if (optionVal) {
      await select.selectOption(optionVal);
      await select.dispatchEvent('change');
    }

    // Ensure displayName is filled
    const displayNameInput = this.page.locator('#displayName');
    const displayVal = await displayNameInput.inputValue();
    if (!displayVal) {
      await displayNameInput.fill(agentName);
    }

    // Submit
    const submitBtn = this.page.getByRole('button', { name: 'Gán Vào Kênh' });
    await submitBtn.click();
    await expect(this.page.locator('.member-type-toggle')).toBeHidden();
  }
}
