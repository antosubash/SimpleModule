import type { Page } from '@playwright/test';
import { expect, test } from '../../fixtures/base';
import { PasskeysPage } from '../../pages/users/passkeys.page';

// Helper: set up a CDP virtual WebAuthn authenticator on the page.
// The virtual authenticator auto-responds to navigator.credentials.create/get
// with isUserVerified:true — no real hardware or biometrics needed.
async function setupVirtualAuthenticator(page: Page) {
  const cdp = await page.context().newCDPSession(page);
  await cdp.send('WebAuthn.enable', { enableUI: false });
  await cdp.send('WebAuthn.addVirtualAuthenticator', {
    options: {
      protocol: 'ctap2',
      transport: 'internal',
      hasResidentKey: true,
      hasUserVerification: true,
      isUserVerified: true,
      automaticPresenceSimulation: true,
    },
  });
  return cdp;
}

test.describe('Passkeys flows', () => {
  test('register a passkey - it appears in list', async ({ page }) => {
    await setupVirtualAuthenticator(page);

    const passkeys = new PasskeysPage(page);
    await passkeys.goto();
    await expect(passkeys.heading).toBeVisible();
    await expect(passkeys.emptyState).toBeVisible();

    await passkeys.addPasskeyButton.click();

    // Virtual authenticator auto-responds; wait for the list to update
    await expect(passkeys.removeButtons.first()).toBeVisible({ timeout: 10_000 });
    await expect(passkeys.removeButtons).not.toHaveCount(0);
  });

  test('delete a passkey - it is removed from list', async ({ page }) => {
    await setupVirtualAuthenticator(page);

    const passkeys = new PasskeysPage(page);
    await passkeys.goto();

    // Register first so there is something to delete
    await passkeys.addPasskeyButton.click();
    await expect(passkeys.removeButtons.first()).toBeVisible({ timeout: 10_000 });

    // Accept the confirm() dialog before clicking Remove
    page.once('dialog', (dialog) => dialog.accept());
    await passkeys.removeButtons.first().click();

    // Passkey should be gone
    await expect(passkeys.emptyState).toBeVisible({ timeout: 5_000 });
  });

  test('register passkey and sign in with it', async ({ page }) => {
    // Start: logged in as admin (via storageState from base fixture)
    await setupVirtualAuthenticator(page);

    // Register a passkey while authenticated
    const passkeys = new PasskeysPage(page);
    await passkeys.goto();
    await passkeys.addPasskeyButton.click();
    await expect(passkeys.removeButtons.first()).toBeVisible({ timeout: 10_000 });

    // Simulate logout by clearing the auth cookie
    // (CDP session stays alive so the virtual authenticator retains the registered credential)
    await page.context().clearCookies();

    // Navigate to login page — passkey button must be visible
    await page.goto('/Identity/Account/Login');
    const passkeySignInButton = page.getByRole('button', { name: /sign in with passkey/i });
    await expect(passkeySignInButton).toBeVisible();

    // Click — virtual authenticator auto-responds with the registered credential
    await passkeySignInButton.click();

    // Successful sign-in redirects to the root (or dashboard)
    await page.waitForURL('/', { timeout: 10_000 });
    await expect(page.locator('body')).toBeVisible();
  });

  test('passkey sign-in cancelled - shows error message', async ({ page }) => {
    // Start unauthenticated
    await page.context().clearCookies();
    await page.goto('/Identity/Account/Login');

    // Set up virtual authenticator with automaticPresenceSimulation: false
    // so that navigator.credentials.get() will be rejected as if the user cancelled
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('WebAuthn.enable', { enableUI: false });
    await cdp.send('WebAuthn.addVirtualAuthenticator', {
      options: {
        protocol: 'ctap2',
        transport: 'internal',
        hasResidentKey: true,
        hasUserVerification: true,
        isUserVerified: true,
        automaticPresenceSimulation: false, // will cause NotAllowedError after timeout
      },
    });

    const passkeySignInButton = page.getByRole('button', { name: /sign in with passkey/i });
    await expect(passkeySignInButton).toBeVisible();
    await passkeySignInButton.click();

    // With no registered credential and no auto-presence, the browser rejects the request.
    // The login page should show an error message.
    await expect(page.getByRole('alert').or(page.getByText(/passkey sign-in/i))).toBeVisible({
      timeout: 10_000,
    });
  });
});
