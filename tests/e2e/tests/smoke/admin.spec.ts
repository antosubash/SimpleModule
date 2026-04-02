import { expect, test } from '../../fixtures/base';
import { AdminRolesPage } from '../../pages/admin/roles.page';
import { AdminRolesCreatePage } from '../../pages/admin/roles-create.page';
import { AdminUsersPage } from '../../pages/admin/users.page';
import { AdminUsersCreatePage } from '../../pages/admin/users-create.page';

test.describe('Admin pages', () => {
  test('users page loads', async ({ page }) => {
    const users = new AdminUsersPage(page);
    await users.goto();
    await expect(users.heading).toBeVisible();
  });

  test('create user page loads', async ({ page }) => {
    const create = new AdminUsersCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });

  test('roles page loads', async ({ page }) => {
    const roles = new AdminRolesPage(page);
    await roles.goto();
    await expect(roles.heading).toBeVisible();
  });

  test('create role page loads', async ({ page }) => {
    const create = new AdminRolesCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });
});
