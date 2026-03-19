import { expect, test } from '../../fixtures/base';
import { ClientsPage } from '../../pages/openiddict/clients.page';

test.describe('OpenIddict Clients', () => {
  test('clients page loads', async ({ page }) => {
    const clientsPage = new ClientsPage(page);
    await clientsPage.goto();
    await expect(clientsPage.heading).toBeVisible();
  });

  test('seeded client is visible', async ({ page }) => {
    const clientsPage = new ClientsPage(page);
    await clientsPage.goto();
    await expect(clientsPage.clientRow('simplemodule-client')).toBeVisible();
  });

  test('create page loads', async ({ page }) => {
    await page.goto('/openiddict/clients/create');
    await expect(page.getByRole('heading', { name: /create client/i })).toBeVisible();
  });

  test('navbar has OAuth Clients link', async ({ page }) => {
    await page.goto('/');
    await expect(page.getByRole('link', { name: 'OAuth Clients' })).toBeVisible();
  });
});

test.describe('OpenIddict Clients - unauthenticated', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('clients page redirects to login', async ({ page }) => {
    await page.goto('/openiddict/clients');
    expect(page.url()).toContain('/Account/Login');
  });
});
