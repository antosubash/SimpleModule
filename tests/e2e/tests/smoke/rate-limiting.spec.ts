import { expect, test } from '../../fixtures/base';
import { RateLimitingAdminPage } from '../../pages/rate-limiting/admin.page';

test.describe('Rate Limiting pages', () => {
  test('admin page loads', async ({ page }) => {
    const admin = new RateLimitingAdminPage(page);
    await admin.goto();
    await expect(admin.heading).toBeVisible();
  });

  test('admin page has stored rules and active policies tabs', async ({ page }) => {
    const admin = new RateLimitingAdminPage(page);
    await admin.goto();
    await expect(admin.storedRulesTab).toBeVisible();
    await expect(admin.activePoliciesTab).toBeVisible();
  });

  test('active policies tab shows policy table', async ({ page }) => {
    const admin = new RateLimitingAdminPage(page);
    await admin.goto();
    await admin.activePoliciesTab.click();
    await expect(admin.activePoliciesTable).toBeVisible();
  });
});
