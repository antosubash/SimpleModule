import { expect, test } from '../../fixtures/base';
import { FeatureFlagsManagePage } from '../../pages/feature-flags/manage.page';

test.describe('Feature Flags pages', () => {
  test('manage page loads', async ({ page }) => {
    const manage = new FeatureFlagsManagePage(page);
    await manage.goto();
    await expect(manage.heading).toBeVisible();
  });

  test('manage page shows flag table', async ({ page }) => {
    const manage = new FeatureFlagsManagePage(page);
    await manage.goto();
    await expect(manage.flagTable).toBeVisible();
  });
});
