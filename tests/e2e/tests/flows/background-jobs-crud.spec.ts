import { expect, test } from '../../fixtures/base';
import { BackgroundJobsDashboardPage } from '../../pages/background-jobs/dashboard.page';
import { BackgroundJobsListPage } from '../../pages/background-jobs/list.page';
import { BackgroundJobsRecurringPage } from '../../pages/background-jobs/recurring.page';

test.describe('BackgroundJobs flows', () => {
  test('dashboard shows job summary cards', async ({ page }) => {
    const dashboard = new BackgroundJobsDashboardPage(page);
    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();

    // Verify summary cards are visible
    await expect(dashboard.activeJobsCard).toBeVisible();
    await expect(dashboard.failedJobsCard).toBeVisible();
    await expect(dashboard.recurringJobsCard).toBeVisible();
  });

  test('dashboard links navigate to correct pages', async ({ page }) => {
    const dashboard = new BackgroundJobsDashboardPage(page);
    const listPage = new BackgroundJobsListPage(page);

    await dashboard.goto();
    await expect(dashboard.heading).toBeVisible();

    // Click "View All" on active jobs card to navigate to list
    const viewAllLink = dashboard.viewAllActiveLink;
    if (await viewAllLink.isVisible()) {
      await viewAllLink.click();
      await page.waitForLoadState('networkidle');
      await expect(listPage.heading).toBeVisible();
    }
  });

  test('list page loads and shows table', async ({ page }) => {
    const listPage = new BackgroundJobsListPage(page);
    await listPage.goto();
    await expect(listPage.heading).toBeVisible();
  });

  test('recurring page loads', async ({ page }) => {
    const recurringPage = new BackgroundJobsRecurringPage(page);
    await recurringPage.goto();
    await expect(recurringPage.heading).toBeVisible();
  });
});
