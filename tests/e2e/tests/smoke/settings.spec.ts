import { expect, test } from '../../fixtures/base';
import { AdminSettingsPage } from '../../pages/settings/admin.page';
import { UserSettingsPage } from '../../pages/settings/user.page';

test.describe('Settings pages', () => {
  test('admin settings page loads', async ({ page }) => {
    const admin = new AdminSettingsPage(page);
    await admin.goto();
    await expect(admin.heading).toBeVisible();
  });

  test('admin settings has system and application tabs', async ({ page }) => {
    const admin = new AdminSettingsPage(page);
    await admin.goto();
    await expect(admin.systemTab).toBeVisible();
    await expect(admin.applicationTab).toBeVisible();
  });

  test('user settings page loads', async ({ page }) => {
    const user = new UserSettingsPage(page);
    await user.goto();
    await expect(user.heading).toBeVisible();
  });
});
