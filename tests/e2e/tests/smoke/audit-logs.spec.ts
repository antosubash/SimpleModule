import { expect, test } from '../../fixtures/base';
import { AuditLogsBrowsePage } from '../../pages/audit-logs/browse.page';
import { AuditLogsDashboardPage } from '../../pages/audit-logs/dashboard.page';

test.describe('AuditLogs pages', () => {
  test('dashboard page loads', async ({ page }) => {
    const dashboard = new AuditLogsDashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();
  });

  test('browse page loads', async ({ page }) => {
    const browse = new AuditLogsBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });
});
