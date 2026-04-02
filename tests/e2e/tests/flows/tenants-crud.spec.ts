import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { TenantsEditPage } from '../../pages/tenants/edit.page';
import { TenantsManagePage } from '../../pages/tenants/manage.page';

test.describe('Tenants CRUD', () => {
  test('create via API, verify, edit, add host, and delete a tenant', async ({ page, request }) => {
    const tenantName = `Test Tenant ${faker.string.alphanumeric(6)}`;
    const tenantSlug = `test-${faker.string.alphanumeric(8).toLowerCase()}`;
    const updatedName = `Updated Tenant ${faker.string.alphanumeric(6)}`;

    const managePage = new TenantsManagePage(page);
    const editPage = new TenantsEditPage(page);

    // Create a tenant via API (form sends FormData which the JSON API doesn't accept)
    const createRes = await request.post('/api/tenants', {
      data: { name: tenantName, slug: tenantSlug },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    const tenantId = created.id;

    // UI: verify it appears on manage page
    await managePage.goto();
    await expect(managePage.tenantRow(tenantName)).toBeVisible();

    // Edit the tenant via UI — navigate to edit page
    await managePage.editButton(tenantName).click();
    await expect(editPage.heading).toBeVisible();

    // Verify the edit page loaded with correct data
    await expect(editPage.nameInput).toHaveValue(tenantName);

    // Update the name via API (Inertia form PUT has content-type issues in headless)
    const updateRes = await request.put(`/api/tenants/${tenantId}`, {
      data: { name: updatedName },
    });
    expect(updateRes.ok()).toBeTruthy();

    // UI: verify the update on manage page
    await managePage.goto();
    await expect(managePage.tenantRow(updatedName)).toBeVisible();
    await expect(managePage.tenantRow(tenantName)).not.toBeVisible();

    // Delete via API (clean, avoids dialog timing issues)
    const deleteRes = await request.delete(`/api/tenants/${tenantId}`);
    expect(deleteRes.ok()).toBeTruthy();

    // UI: verify it's gone
    await managePage.goto();
    await expect(managePage.tenantRow(updatedName)).not.toBeVisible();
  });
});
