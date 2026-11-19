import { test, expect } from '@playwright/test';
import { LoginPage } from '../pages/login.page';
import { OrganizationPage } from '../pages/organization.page';

test.describe('Sprint 0 E2E: Organization & Agent Profiles', () => {
  let loginPage: LoginPage;
  let orgPage: OrganizationPage;

  test.beforeEach(async ({ page }) => {
    loginPage = new LoginPage(page);
    orgPage = new OrganizationPage(page);
  });

  test('Complete Journey: Login -> Create Dept -> Create Agent Profile -> Create Channel -> Assign Agent', async ({ page }) => {
    // 1. Login with seeded SuperAdmin credentials
    await loginPage.goto();
    await loginPage.login('admin@mvp.vn', 'Admin@2026');

    // 2. Expect redirection to Dashboard
    await expect(page).toHaveURL(/.*dashboard/);

    // 3. Navigate to Organization feature
    await orgPage.goto();
    await expect(page).toHaveURL(/.*organization/);

    // 4. Create an Agent Profile first
    const agentName = `Auto Agent ${Date.now()}`;
    await orgPage.createAgentProfile(
      agentName,
      'gemini-2.5-flash',
      'You are an expert autonomous assistant for testing.'
    );

    // 5. Create a new Department
    const deptName = `Dept ${Date.now()}`;
    await orgPage.createDepartment(deptName, 'Automated E2E Testing Department');

    // Verify department exists in the list
    const deptLocator = page.locator('.dept-item').filter({ hasText: deptName });
    await expect(deptLocator).toBeVisible();

    // 6. Select department and create a new Team Channel
    await orgPage.selectDepartment(deptName);
    const channelName = 'e2e-channel';
    await orgPage.createChannel(channelName, 'Channel for E2E testing');

    // Verify channel exists in the list
    const channelLocator = page.locator('.channel-item').filter({ hasText: channelName });
    await expect(channelLocator).toBeVisible();

    // 7. Select the channel
    await orgPage.selectChannel(channelName);
    await expect(page.locator('.channel-header-bar h2')).toContainText(channelName);

    // 8. Assign the created Agent into the channel
    await orgPage.assignAgentToChannel(agentName);

    // 9. Verify member appears with .badge-agent in the channel
    const memberCard = page.locator('.member-card').filter({ hasText: agentName });
    await expect(memberCard).toBeVisible();
    await expect(memberCard.locator('.badge-agent')).toBeVisible();
  });
});
