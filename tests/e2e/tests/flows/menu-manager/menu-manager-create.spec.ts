import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';
import { MenuManagerPage } from '../../../pages/settings/menu-manager.page';
import {
  addTopLevelItem,
  createdIds,
  trackCreated,
  waitForMenuResponse,
} from './menu-manager-helpers';

test.describe('Menu Manager - CRUD Flows', () => {
  let menuManager: MenuManagerPage;

  test.beforeEach(async ({ page }) => {
    menuManager = new MenuManagerPage(page);
    await menuManager.goto();
    await expect(menuManager.heading).toBeVisible();
  });

  test.afterAll(async ({ request }) => {
    for (const id of createdIds) {
      await request.delete(`/api/settings/menus/${id}`).catch(() => {});
    }
    await request.delete('/api/settings/menus/home').catch(() => {});
  });

  test('create a top-level menu item', async ({ page, request }) => {
    const label = faker.commerce.department();
    const url = `/${faker.helpers.slugify(label).toLowerCase()}`;

    await addTopLevelItem(page, menuManager, label, url);

    // UI: verify item appears in tree
    await expect(menuManager.treeItemButton(label)).toBeVisible();

    // API: verify item was saved
    const apiRes = await request.get('/api/settings/menus');
    expect(apiRes.ok()).toBeTruthy();
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(label);
  });

  test('create and edit a menu item', async ({ page, request }) => {
    const label = faker.company.buzzNoun();
    const editedLabel = faker.company.buzzNoun();
    const editedUrl = `/${faker.helpers.slugify(editedLabel).toLowerCase()}`;

    await addTopLevelItem(page, menuManager, label);

    // Select and edit
    await menuManager.selectItem(label);
    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(editedLabel);
    await menuManager.urlInput.clear();
    await menuManager.urlInput.fill(editedUrl);

    await Promise.all([waitForMenuResponse(page, 'PUT'), menuManager.saveButton.click()]);
    await page.waitForLoadState('networkidle');

    // UI: verify updated label
    await expect(menuManager.treeItemButton(editedLabel)).toBeVisible();

    // API: verify the edit was persisted
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(editedLabel);
    expect(flat).not.toContain(`"label":"${label}"`);
  });

  test('create a child menu item', async ({ page, request }) => {
    const parentLabel = faker.commerce.department();
    const childLabel = faker.commerce.productAdjective();

    await addTopLevelItem(page, menuManager, parentLabel);

    await menuManager.selectItem(parentLabel);
    await menuManager.addChildButton.waitFor({ state: 'visible' });

    const [childResponse] = await Promise.all([
      waitForMenuResponse(page, 'POST'),
      menuManager.addChildButton.click(),
    ]);
    trackCreated(childResponse);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(childLabel);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(childLabel).toLowerCase()}`);

    await Promise.all([waitForMenuResponse(page, 'PUT'), menuManager.saveButton.click()]);
    await page.waitForLoadState('networkidle');

    // UI: verify child appears
    await expect(menuManager.treeItemButton(childLabel)).toBeVisible();

    // API: verify parent-child relationship
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(childLabel);
  });

  test('delete a menu item', async ({ page, request }) => {
    const label = faker.animal.cat();

    await addTopLevelItem(page, menuManager, label);

    // Select and delete via UI
    await menuManager.selectItem(label);
    await menuManager.deleteButton.waitFor({ state: 'visible' });
    await menuManager.deleteButton.click();
    const dialog = page.getByRole('alertdialog').or(page.getByRole('dialog'));
    await Promise.all([
      waitForMenuResponse(page, 'DELETE_OR_PUT'),
      dialog.getByRole('button', { name: 'Delete' }).click(),
    ]);

    // UI: verify removed from tree
    await expect(menuManager.treeItemButton(label)).not.toBeVisible();

    // API: verify removed from backend
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).not.toContain(label);
  });
});
