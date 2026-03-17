import { expect, test } from '../../fixtures/base';
import { DashboardPage } from '../../pages/dashboard.page';

test.describe('Dashboard', () => {
  test('home page loads', async ({ page }) => {
    const dashboard = new DashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();
  });
});
