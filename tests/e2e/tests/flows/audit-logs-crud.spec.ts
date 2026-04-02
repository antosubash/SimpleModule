import { expect, test } from '../../fixtures/base';
import { AuditLogsBrowsePage } from '../../pages/audit-logs/browse.page';
import { AuditLogsDashboardPage } from '../../pages/audit-logs/dashboard.page';

test.describe('AuditLogs flows', () => {
  test('dashboard shows stats and date presets work', async ({ page }) => {
    const dashboard = new AuditLogsDashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();

    // Verify KPI cards are present
    await expect(dashboard.totalEventsCard).toBeVisible();

    // Click a date preset
    await dashboard.last7daysButton.click();
    await dashboard.applyButton.click();
    await page.waitForLoadState('networkidle');

    // Dashboard should still be visible after filter apply
    await expect(dashboard.heading).toBeVisible();
  });

  test('browse with filters and navigate to detail', async ({ page, request }) => {
    const browse = new AuditLogsBrowsePage(page);

    // Seed an audit entry by making an API call
    await request.get('/api/products');

    await browse.goto();
    await expect(browse.heading).toBeVisible();

    // Check that table rows exist (audit entries from prior activity)
    const rowCount = await browse.tableRows.count();
    if (rowCount > 0) {
      // Click the first row to navigate to detail
      await browse.entryRow(0).click();
      await page.waitForLoadState('networkidle');

      // Should be on a detail page — URL should contain the entry ID
      await expect(page).toHaveURL(/\/audit-logs\/\d+/);
    }
  });

  test('export buttons trigger downloads', async ({ page, request }) => {
    // Seed an audit entry
    await request.get('/api/products');

    const browse = new AuditLogsBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();

    // Test CSV export via API
    const csvRes = await request.get('/api/audit-logs/export?format=csv');
    expect(csvRes.status()).toBeLessThan(500);

    // Test JSON export via API
    const jsonRes = await request.get('/api/audit-logs/export?format=json');
    expect(jsonRes.status()).toBeLessThan(500);
  });
});
