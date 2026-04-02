import { expect, test } from '../../fixtures/base';
import { BackgroundJobsDashboardPage } from '../../pages/background-jobs/dashboard.page';
import { BackgroundJobsListPage } from '../../pages/background-jobs/list.page';
import { BackgroundJobsRecurringPage } from '../../pages/background-jobs/recurring.page';

test.describe('BackgroundJobs pages', () => {
  test('dashboard page loads', async ({ page }) => {
    const dashboard = new BackgroundJobsDashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();
  });

  test('list page loads', async ({ page }) => {
    const list = new BackgroundJobsListPage(page);
    await list.goto();
    await expect(list.heading).toBeVisible();
  });

  test('recurring page loads', async ({ page }) => {
    const recurring = new BackgroundJobsRecurringPage(page);
    await recurring.goto();
    await expect(recurring.heading).toBeVisible();
  });
});
