import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { AdminRolesPage } from '../../pages/admin/roles.page';
import { AdminRolesCreatePage } from '../../pages/admin/roles-create.page';
import { AdminRolesEditPage } from '../../pages/admin/roles-edit.page';

test.describe('Admin Roles CRUD', () => {
  test('create, verify, edit, and delete a role', async ({ page }) => {
    const roleName = `TestRole_${faker.string.alphanumeric(8)}`;
    const roleDescription = faker.lorem.sentence();
    const updatedName = `TestRole_${faker.string.alphanumeric(8)}`;

    const rolesPage = new AdminRolesPage(page);
    const createPage = new AdminRolesCreatePage(page);
    const editPage = new AdminRolesEditPage(page);

    // Create a role via UI
    await createPage.goto();
    await expect(createPage.heading).toBeVisible();
    await createPage.createRole(roleName, roleDescription);

    // Should redirect to edit page
    await expect(editPage.heading).toBeVisible();

    // UI: verify it appears on roles list
    await rolesPage.goto();
    await expect(rolesPage.roleRow(roleName)).toBeVisible();

    // Edit the role name via UI
    await rolesPage.editButton(roleName).click();
    await expect(editPage.heading).toBeVisible();
    await editPage.updateName(updatedName);

    // UI: verify the update on roles list
    await rolesPage.goto();
    await expect(rolesPage.roleRow(updatedName)).toBeVisible();
    await expect(rolesPage.roleRow(roleName)).not.toBeVisible();

    // Delete the role via UI
    await rolesPage.deleteButton(updatedName).click();
    await rolesPage.confirmDeleteButton.click();
    await page.waitForLoadState('networkidle');

    // UI: verify it's gone
    await expect(rolesPage.roleRow(updatedName)).not.toBeVisible();
  });

  test('edit role permissions tab', async ({ page }) => {
    const rolesPage = new AdminRolesPage(page);
    const editPage = new AdminRolesEditPage(page);

    await rolesPage.goto();

    // Find and click edit on the first non-Admin role if available
    const firstEditButton = page
      .getByRole('row')
      .filter({ hasNotText: /^Admin$/ })
      .getByRole('button', { name: /edit/i })
      .or(page.getByRole('link', { name: /edit/i }))
      .first();

    if (await firstEditButton.isVisible()) {
      await firstEditButton.click();
      await expect(editPage.heading).toBeVisible();

      // Navigate to permissions tab
      await editPage.permissionsTab.click();
      await expect(page.getByText(/permissions/i)).toBeVisible();
    }
  });
});
