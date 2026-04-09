import { expect, test } from '../../fixtures/base';
import {
  EmailCreateTemplatePage,
  EmailDashboardPage,
  EmailHistoryPage,
  EmailSettingsPage,
  EmailTemplatesPage,
} from '../../pages/email/email.page';

test.describe('Email pages', () => {
  test('dashboard page loads', async ({ page }) => {
    const dashboard = new EmailDashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();
  });

  test('templates page loads', async ({ page }) => {
    const templates = new EmailTemplatesPage(page);
    await templates.goto();
    await expect(templates.heading).toBeVisible();
  });

  test('templates page shows new-template action', async ({ page }) => {
    const templates = new EmailTemplatesPage(page);
    await templates.goto();
    await expect(templates.newTemplateButton).toBeVisible();
  });

  test('create template page loads', async ({ page }) => {
    const create = new EmailCreateTemplatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });

  test('history page loads', async ({ page }) => {
    const history = new EmailHistoryPage(page);
    await history.goto();
    await expect(history.heading).toBeVisible();
  });

  test('settings page loads', async ({ page }) => {
    const settings = new EmailSettingsPage(page);
    await settings.goto();
    await expect(settings.heading).toBeVisible();
  });
});
