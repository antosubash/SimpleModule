import { expect, test } from '../../fixtures/base';
import { PasskeysPage } from '../../pages/users/passkeys.page';
import { TwoFactorPage } from '../../pages/users/two-factor.page';

test.describe('Users account pages', () => {
  test('two-factor authentication page loads', async ({ page }) => {
    const twoFactor = new TwoFactorPage(page);
    await twoFactor.goto();
    await expect(twoFactor.heading).toBeVisible();
  });

  test('passkeys management page loads', async ({ page }) => {
    const passkeys = new PasskeysPage(page);
    await passkeys.goto();
    await expect(passkeys.heading).toBeVisible();
  });

  test('passkeys page shows add passkey button', async ({ page }) => {
    const passkeys = new PasskeysPage(page);
    await passkeys.goto();
    await expect(passkeys.addPasskeyButton).toBeVisible();
  });

  test('login page shows sign in with passkey button', async ({ page }) => {
    // Sign out first so we can see the login page properly
    await page.context().clearCookies();
    await page.goto('/Identity/Account/Login');
    await expect(page.getByRole('button', { name: /sign in with passkey/i })).toBeVisible();
  });
});
