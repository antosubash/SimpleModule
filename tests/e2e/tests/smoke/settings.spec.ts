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

  test('search filters settings by display name', async ({ page }) => {
    const admin = new AdminSettingsPage(page);
    await admin.goto();
    await admin.applicationTab.click();
    const search = page.getByPlaceholder(/search settings/i);
    await search.fill('primary color');
    await expect(page.getByText(/^Primary Color$/)).toBeVisible();
    await expect(page.getByText(/^Max File Size/)).toHaveCount(0);
  });

  test('color setting renders hex input and swatch', async ({ page }) => {
    const admin = new AdminSettingsPage(page);
    await admin.goto();
    await admin.applicationTab.click();
    await page.getByPlaceholder(/search settings/i).fill('primary color');
    await expect(page.locator('input[type="color"]')).toBeVisible();
    await expect(page.locator('input[maxlength="7"]')).toHaveValue(/^#[0-9a-fA-F]{6}$/);
  });

  test('select setting populates options from allowedValues and saves', async ({ page }) => {
    const user = new UserSettingsPage(page);
    await user.goto();
    // The Display Density setting is a Select with allowedValues compact/comfortable/spacious.
    // Save baseline so we can assert it changes, then restore via reset to be idempotent.
    const before = await page.request.get('/api/settings/user.preferred_density/resolved');
    expect(before.ok()).toBeTruthy();

    const trigger = page.getByRole('combobox', { name: /display density/i });
    await trigger.click();
    await expect(page.getByRole('option', { name: 'compact' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'comfortable' })).toBeVisible();
    await expect(page.getByRole('option', { name: 'spacious' })).toBeVisible();
    await page.getByRole('option', { name: 'spacious' }).click();

    // Verify via API that the override was actually persisted with the decoded string value.
    await expect(async () => {
      const r = await page.request.get('/api/settings/user.preferred_density/resolved');
      expect(r.ok()).toBeTruthy();
      const body = (await r.json()) as { value: unknown };
      expect(body.value).toBe('spacious');
    }).toPass({ timeout: 5000 });

    // Cleanup so the test is idempotent across runs.
    await page.request.delete('/api/settings/me/user.preferred_density');
  });

  test('user settings show inheritance line for unset values', async ({ page }) => {
    const user = new UserSettingsPage(page);
    await user.goto();
    await expect(page.getByText(/Current:/).first()).toBeVisible();
    await expect(page.getByText(/inherited default/i).first()).toBeVisible();
  });

  test('admin api returns decoded values not double-encoded strings', async ({ page }) => {
    const response = await page.request.get('/api/settings');
    expect(response.ok()).toBeTruthy();
    const body = (await response.json()) as Array<{ value: unknown }>;
    // Any non-null value must be the decoded primitive/object, never a JSON-encoded string.
    for (const entry of body) {
      if (typeof entry.value === 'string') {
        // The string itself must not start with a JSON-encoded quote
        expect(entry.value.startsWith('"') && entry.value.endsWith('"')).toBeFalsy();
      }
    }
  });
});
