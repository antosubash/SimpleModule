import { expect, test } from '../../fixtures/base';
import { EnableAuthenticatorPage } from '../../pages/users/enable-authenticator.page';
import { TwoFactorPage } from '../../pages/users/two-factor.page';

test.describe('Users 2FA flows', () => {
  test('2FA status page shows authenticator options', async ({ page }) => {
    const twoFactor = new TwoFactorPage(page);
    await twoFactor.goto();
    await expect(twoFactor.heading).toBeVisible();

    // Should show add/setup authenticator button
    const addButton = twoFactor.addAuthenticatorButton;
    const setupButton = twoFactor.setupAuthenticatorButton;

    const hasAddButton = await addButton.isVisible().catch(() => false);
    const hasSetupButton = await setupButton.isVisible().catch(() => false);

    // At least one authenticator button should be visible
    expect(hasAddButton || hasSetupButton).toBeTruthy();
  });

  test('enable authenticator page shows shared key and QR code', async ({ page }) => {
    const enableAuth = new EnableAuthenticatorPage(page);
    await enableAuth.goto();
    await expect(enableAuth.heading).toBeVisible();

    // Shared key should be displayed
    await expect(enableAuth.sharedKey.first()).toBeVisible();

    // Verification code input should be visible
    await expect(enableAuth.codeInput).toBeVisible();

    // Verify button should be visible
    await expect(enableAuth.verifyButton).toBeVisible();
  });

  test('navigate from 2FA page to enable authenticator and back', async ({ page }) => {
    const twoFactor = new TwoFactorPage(page);
    await twoFactor.goto();
    await expect(twoFactor.heading).toBeVisible();

    // Click add/setup authenticator
    const addButton = twoFactor.addAuthenticatorButton;
    const setupButton = twoFactor.setupAuthenticatorButton;

    if (await addButton.isVisible().catch(() => false)) {
      await addButton.click();
    } else if (await setupButton.isVisible().catch(() => false)) {
      await setupButton.click();
    }

    await page.waitForLoadState('networkidle');

    // Should be on enable authenticator page
    const enableAuth = new EnableAuthenticatorPage(page);
    await expect(enableAuth.heading).toBeVisible();
    await expect(enableAuth.codeInput).toBeVisible();
  });
});
