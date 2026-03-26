import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import {
  ClientsCreatePage,
  ClientsEditPage,
  ClientsPage,
} from '../../pages/openiddict/clients.page';

test.describe('OpenIddict Clients CRUD', () => {
  test('create, verify, edit, and delete a client', async ({ page }) => {
    const clientId = faker.string.alphanumeric({ length: 16, casing: 'lower' });
    const displayName = faker.company.name();
    const updatedName = faker.company.name();

    const clientsPage = new ClientsPage(page);
    const createPage = new ClientsCreatePage(page);
    const editPage = new ClientsEditPage(page);

    // Create a client via UI
    await createPage.goto();
    await expect(createPage.heading).toBeVisible();
    await createPage.createClient(clientId, displayName);

    // UI: should redirect to edit page
    await expect(editPage.heading).toBeVisible();

    // UI: verify it appears on list page
    await clientsPage.goto();
    await clientsPage.showAllRows();
    await expect(clientsPage.clientRow(clientId)).toBeVisible();

    // API: verify the client was persisted via Inertia page props
    const listRes = await page.request.get('https://localhost:5001/openiddict/clients', {
      headers: { 'X-Inertia': 'true', 'X-Inertia-Version': '' },
    });
    if (listRes.ok()) {
      const body = await listRes.json();
      if (body?.props?.clients) {
        const apiClient = body.props.clients.find(
          (c: { clientId: string }) => c.clientId === clientId,
        );
        expect(apiClient).toBeTruthy();
        expect(apiClient.displayName).toBe(displayName);
      }
    }

    // Edit the client display name via UI
    await clientsPage.editButton(clientId).click();
    await expect(editPage.heading).toBeVisible();
    await editPage.updateDisplayName(updatedName);

    // UI: verify the update on list page
    await clientsPage.goto();
    await clientsPage.showAllRows();
    await expect(clientsPage.clientRow(clientId)).toContainText(updatedName, { timeout: 10000 });

    // Delete the client via UI
    page.on('dialog', (dialog) => dialog.accept());
    await clientsPage.deleteButton(clientId).click();
    await page.waitForLoadState('networkidle');

    // UI: verify it's gone
    await expect(clientsPage.clientRow(clientId)).not.toBeVisible();
  });

  test('edit client URIs tab', async ({ page }) => {
    const clientsPage = new ClientsPage(page);
    const editPage = new ClientsEditPage(page);

    await clientsPage.goto();
    await clientsPage.showAllRows();
    await clientsPage.editButton('simplemodule-client').click();
    await expect(editPage.heading).toBeVisible();

    await editPage.urisTab.click();
    await expect(page.getByRole('heading', { name: /redirect uris/i })).toBeVisible();

    await expect(page.locator('input[value*="swagger/oauth2-redirect"]')).toBeVisible();
    await expect(page.locator('input[value*="oauth-callback"]')).toBeVisible();
  });

  test('edit client Permissions tab', async ({ page }) => {
    const clientsPage = new ClientsPage(page);
    const editPage = new ClientsEditPage(page);

    await clientsPage.goto();
    await clientsPage.showAllRows();
    await clientsPage.editButton('simplemodule-client').click();
    await expect(editPage.heading).toBeVisible();

    await editPage.permissionsTab.click();

    await expect(page.getByText('Endpoints')).toBeVisible();
    await expect(page.getByText('Grant Types')).toBeVisible();
    await expect(page.getByText('Response Types')).toBeVisible();
    await expect(page.getByText('Scopes')).toBeVisible();
  });
});
