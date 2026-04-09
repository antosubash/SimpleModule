import type { CDPSession, Page } from '@playwright/test';
import { expect, test } from '../../fixtures/base';
import { PasskeysPage } from '../../pages/users/passkeys.page';

// CDP returns byte arrays as standard base64; WebAuthn needs base64url (no +, /, or =).
function cdpBase64ToBase64Url(base64: string): string {
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}

// Helper: set up a CDP virtual WebAuthn authenticator on the page.
// The virtual authenticator auto-responds to navigator.credentials.create/get
// with isUserVerified:true — no real hardware or biometrics needed.
// Returns the CDP session and authenticator ID for follow-up CDP calls.
async function setupVirtualAuthenticator(
  page: Page,
): Promise<{ cdp: CDPSession; authenticatorId: string }> {
  const cdp = await page.context().newCDPSession(page);
  await cdp.send('WebAuthn.enable', { enableUI: false });
  const { authenticatorId } = (await cdp.send('WebAuthn.addVirtualAuthenticator', {
    options: {
      protocol: 'ctap2',
      transport: 'internal',
      hasResidentKey: true,
      hasUserVerification: true,
      isUserVerified: true,
      automaticPresenceSimulation: true,
    },
  })) as { authenticatorId: string };
  return { cdp, authenticatorId };
}

test.describe('Passkeys flows', () => {
  // Run tests serially so they don't race on the shared admin user's passkey list
  test.describe.configure({ mode: 'serial' });

  // Clean up all passkeys before each test to start from a known state.
  // Passkeys accumulate across runs (file-based SQLite) and parallel suites.
  // We delete via the API rather than clicking Remove in the UI because the
  // UI uses window.confirm() + router.reload(), which races with the test's
  // own dialog handling when there are multiple passkeys to clean up.
  test.beforeEach(async ({ request }) => {
    const listRes = await request.get('/api/passkeys');
    if (!listRes.ok()) return;
    const existing = (await listRes.json()) as Array<{ credentialId: string }>;
    await Promise.all(
      existing.map((p) => request.delete(`/api/passkeys/${encodeURIComponent(p.credentialId)}`)),
    );
  });

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

    // Passkey should be gone (beforeEach guarantees we started from empty)
    await expect(passkeys.emptyState).toBeVisible({ timeout: 5_000 });
  });

  test('register passkey and sign in with it', async ({ page }) => {
    // Start: logged in as admin (via storageState from base fixture)
    const { cdp, authenticatorId } = await setupVirtualAuthenticator(page);

    // Register a passkey while authenticated
    const passkeys = new PasskeysPage(page);
    await passkeys.goto();
    await passkeys.addPasskeyButton.click();
    await expect(passkeys.removeButtons.first()).toBeVisible({ timeout: 10_000 });

    // Read the registered credential ID from the virtual authenticator.
    // Chrome's virtual authenticator persists across same-origin navigations
    // (same browser tab = same CDP target), but the credential may not have been
    // stored as a discoverable/resident key depending on the server's creation options.
    // We include it explicitly in allowCredentials to guarantee the authenticator
    // can find it regardless of whether it is resident.
    const { credentials } = (await cdp.send('WebAuthn.getCredentials', {
      authenticatorId,
    })) as { credentials: Array<{ credentialId: string }> };
    const credentialIdBase64url = cdpBase64ToBase64Url(credentials[0]?.credentialId ?? '');

    // Intercept the assertion-begin response to add this credential's ID to allowCredentials.
    // Registered before navigation so no request can slip through on page load.
    // The challenge cookie is set by the real server response (route.fetch forwards headers).
    await page.route('**/api/passkeys/login/begin', async (route) => {
      const response = await route.fetch();
      const body = (await response.json()) as Record<string, unknown>;
      if (credentialIdBase64url) {
        body.allowCredentials = [{ type: 'public-key', id: credentialIdBase64url }];
      }
      await route.fulfill({ response, json: body });
    });

    // Simulate logout by clearing auth cookies
    await page.context().clearCookies();

    // Navigate to login page — the virtual authenticator persists (same browser tab)
    await page.goto('/Identity/Account/Login');

    const passkeySignInButton = page.getByRole('button', { name: /sign in with passkey/i });
    await expect(passkeySignInButton).toBeVisible();

    // Click — virtual authenticator auto-responds with the registered credential
    await passkeySignInButton.click();

    // Successful sign-in redirects to '/'. If it fails, surface the alert text as the error.
    try {
      await page.waitForURL('/', { timeout: 12_000 });
    } catch {
      const alertText = await page
        .getByRole('alert')
        .textContent()
        .catch(() => null);
      throw new Error(
        alertText ? `Passkey sign-in failed: ${alertText}` : 'No redirect to / and no error alert',
      );
    }
    await expect(page.locator('body')).toBeVisible();
  });

  test('passkey sign-in failure - shows error message', async ({ page }) => {
    // Start unauthenticated
    await page.context().clearCookies();

    // Mock the begin endpoint to fail immediately — simulates any network or server error
    // that prevents passkey sign-in from starting. This is more reliable than relying on
    // the virtual authenticator's NotAllowedError timing (which varies across Chrome versions).
    await page.route('**/api/passkeys/login/begin', (route) =>
      route.fulfill({ status: 500, body: 'Internal Server Error' }),
    );

    await page.goto('/Identity/Account/Login');
    const passkeySignInButton = page.getByRole('button', { name: /sign in with passkey/i });
    await expect(passkeySignInButton).toBeVisible();
    await passkeySignInButton.click();

    // The login page should show an error alert
    await expect(page.getByRole('alert')).toBeVisible({ timeout: 5_000 });
  });
});
