import { expect, test } from '../../fixtures/base';
import { TenantsBrowsePage } from '../../pages/tenants/browse.page';
import { TenantsCreatePage } from '../../pages/tenants/create.page';
import { TenantsManagePage } from '../../pages/tenants/manage.page';

test.describe('Tenants pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new TenantsBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });

  test('manage page loads', async ({ page }) => {
    const manage = new TenantsManagePage(page);
    await manage.goto();
    await expect(manage.heading).toBeVisible();
  });

  test('create page loads', async ({ page }) => {
    const create = new TenantsCreatePage(page);
    await create.goto();
    await expect(create.heading).toBeVisible();
  });
});
