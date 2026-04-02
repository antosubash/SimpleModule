import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { AdminUsersPage } from '../../pages/admin/users.page';
import { AdminUsersCreatePage } from '../../pages/admin/users-create.page';
import { AdminUsersEditPage } from '../../pages/admin/users-edit.page';

test.describe('Admin Users CRUD', () => {
  let createdUserEmail: string;

  test.afterAll(async ({ request }) => {
    // Clean up: attempt to find and delete the test user via API
    if (createdUserEmail) {
      const res = await request.get('/admin/users', {
        headers: { 'X-Inertia': 'true', 'X-Inertia-Version': '' },
      });
      if (res.ok()) {
        const body = await res.json();
        const users = body?.props?.users ?? [];
        const testUser = users.find((u: { email: string }) => u.email === createdUserEmail);
        if (testUser) {
          await request.post(`/admin/users/${testUser.id}/deactivate`);
        }
      }
    }
  });

  test('create, verify, and edit a user', async ({ page }) => {
    const displayName = faker.person.fullName();
    createdUserEmail = `test-${faker.string.alphanumeric(8)}@e2e-test.local`;
    const password = 'TestPass123!';
    const updatedName = faker.person.fullName();

    const usersPage = new AdminUsersPage(page);
    const createPage = new AdminUsersCreatePage(page);
    const editPage = new AdminUsersEditPage(page);

    // Create a user via UI
    await createPage.goto();
    await expect(createPage.heading).toBeVisible();
    await createPage.createUser(displayName, createdUserEmail, password, {
      confirmEmail: true,
    });

    // Should redirect to edit page
    await expect(editPage.heading).toBeVisible();

    // UI: verify it appears on users list
    await usersPage.goto();
    await expect(usersPage.userRow(createdUserEmail)).toBeVisible();

    // Edit the user display name via UI
    await usersPage.editButton(createdUserEmail).click();
    await expect(editPage.heading).toBeVisible();
    await editPage.updateDisplayName(updatedName);

    // Navigate to roles tab
    await editPage.rolesTab.click();
    await expect(editPage.rolesTab).toHaveAttribute('data-state', 'active');

    // Navigate to security tab
    await editPage.securityTab.click();
    await expect(editPage.securityTab).toHaveAttribute('data-state', 'active');

    // Navigate to sessions tab
    await editPage.sessionsTab.click();
    await expect(editPage.sessionsTab).toHaveAttribute('data-state', 'active');
  });

  test('search users', async ({ page }) => {
    const usersPage = new AdminUsersPage(page);

    await usersPage.goto();
    await expect(usersPage.heading).toBeVisible();

    // Search for the admin user
    await usersPage.search('admin');
    await expect(usersPage.userRow('admin@simplemodule.dev')).toBeVisible();
  });
});
