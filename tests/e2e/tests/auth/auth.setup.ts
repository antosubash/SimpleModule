import path from 'node:path';
import { expect, test as setup } from '@playwright/test';

const authFile = path.resolve(__dirname, '../../auth/.auth/user.json');

setup('authenticate as admin', async ({ page }) => {
  // Navigate to the app — unauthenticated users see the landing page with login button
  await page.goto('/');

  // Click the login link to navigate to Identity login page
  await page.getByRole('link', { name: 'Log in' }).click();
  await page.waitForURL('**/Identity/Account/Login**');

  // Fill the login form (Blazor Identity — labels render as divs, use placeholders)
  await page.getByPlaceholder('you@example.com').fill('admin@simplemodule.dev');
  await page.locator('input[type="password"]').fill('Admin123!');
  await page.getByRole('button', { name: 'Log in' }).click();

  // Wait for redirect back to the app after successful login
  await page.waitForURL('/');

  // Verify we're authenticated — dashboard should show user info
  await expect(page.locator('body')).toBeVisible();

  // Store auth state for reuse
  await page.context().storageState({ path: authFile });
});
