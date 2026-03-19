import { expect, test } from '../../fixtures/base';
import {
  ClientsCreatePage,
  ClientsEditPage,
  ClientsPage,
} from '../../pages/openiddict/clients.page';

test.describe('OpenIddict Clients CRUD', () => {
  test('create, verify, edit, and delete a client', async ({ page }) => {
    const suffix = Date.now();
    const clientId = `e2e-client-${suffix}`;
    const displayName = `E2E Test Client ${suffix}`;
    const updatedName = `E2E Updated Client ${suffix}`;

    const clientsPage = new ClientsPage(page);
    const createPage = new ClientsCreatePage(page);
    const editPage = new ClientsEditPage(page);

    // Create a client
    await createPage.goto();
    await expect(createPage.heading).toBeVisible();
    await createPage.createClient(clientId, displayName);

    // Should redirect to edit page
    await expect(editPage.heading).toBeVisible();

    // Verify it appears on list page (search by clientId which is always in the row)
    await clientsPage.goto();
    await expect(clientsPage.clientRow(clientId)).toBeVisible();

    // Edit the client display name
    await clientsPage.editButton(clientId).click();
    await expect(editPage.heading).toBeVisible();
    await editPage.updateDisplayName(updatedName);

    // Verify the update on list page (the clientId row should now contain the updated name)
    await clientsPage.goto();
    await expect(clientsPage.clientRow(clientId)).toBeVisible();
    // The display name column should show the updated name
    await expect(clientsPage.clientRow(clientId)).toContainText(updatedName, { timeout: 10000 });

    // Delete the client
    page.on('dialog', (dialog) => dialog.accept());
    await clientsPage.deleteButton(clientId).click();
    await page.waitForLoadState('networkidle');

    // Verify it's gone
    await expect(clientsPage.clientRow(clientId)).not.toBeVisible();
  });

  test('edit client URIs tab', async ({ page }) => {
    const clientsPage = new ClientsPage(page);
    const editPage = new ClientsEditPage(page);

    // Navigate to the seeded client's edit page
    await clientsPage.goto();
    await clientsPage.editButton('simplemodule-client').click();
    await expect(editPage.heading).toBeVisible();

    // Switch to URIs tab
    await editPage.urisTab.click();
    await expect(page.getByRole('heading', { name: /redirect uris/i })).toBeVisible();

    // Verify seeded redirect URIs are displayed in input fields
    await expect(page.locator('input[value*="swagger/oauth2-redirect"]')).toBeVisible();
    await expect(page.locator('input[value*="oauth-callback"]')).toBeVisible();
  });

  test('edit client Permissions tab', async ({ page }) => {
    const clientsPage = new ClientsPage(page);
    const editPage = new ClientsEditPage(page);

    // Navigate to the seeded client's edit page
    await clientsPage.goto();
    await clientsPage.editButton('simplemodule-client').click();
    await expect(editPage.heading).toBeVisible();

    // Switch to Permissions tab
    await editPage.permissionsTab.click();

    // Verify permission groups are visible
    await expect(page.getByText('Endpoints')).toBeVisible();
    await expect(page.getByText('Grant Types')).toBeVisible();
    await expect(page.getByText('Response Types')).toBeVisible();
    await expect(page.getByText('Scopes')).toBeVisible();
  });
});
