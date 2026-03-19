import { expect, test } from '../../fixtures/base';
import { AdminSettingsPage } from '../../pages/settings/admin.page';
import { UserSettingsPage } from '../../pages/settings/user.page';

test.describe('Settings CRUD flows', () => {
  test('can switch between system and application tabs', async ({ page }) => {
    const admin = new AdminSettingsPage(page);
    await admin.goto();

    await admin.applicationTab.click();
    await expect(admin.settingCards.first()).toBeVisible();

    await admin.systemTab.click();
    await expect(admin.settingCards.first()).toBeVisible();
  });

  test('user settings shows default badges initially', async ({ page }) => {
    const user = new UserSettingsPage(page);
    await user.goto();
    await expect(user.getBadge('default').first()).toBeVisible();
  });
});
