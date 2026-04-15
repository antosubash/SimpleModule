import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';
import { MenuManagerPage } from '../../../pages/settings/menu-manager.page';
import {
  addTopLevelItem,
  createdIds,
  trackCreated,
  waitForMenuResponse,
} from './menu-manager-helpers';

test.describe('Menu Manager - Settings Flows', () => {
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

  test('toggle visibility of a menu item', async ({ page, request }) => {
    const label = faker.music.genre();

    await addTopLevelItem(page, menuManager, label);

    await menuManager.selectItem(label);
    await menuManager.settingsTab.click();
    await menuManager.visibleSwitch.waitFor({ state: 'visible' });
    await menuManager.visibleSwitch.click();

    await Promise.all([waitForMenuResponse(page, 'PUT'), menuManager.saveButton.click()]);
    await page.waitForLoadState('networkidle');

    // API: verify visibility was toggled off
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(`"isVisible":false`);
  });

  test('set home page on a menu item', async ({ page, request }) => {
    const label = faker.location.city();

    await addTopLevelItem(page, menuManager, label);

    await menuManager.selectItem(label);
    await menuManager.settingsTab.click();
    await menuManager.homePageSwitch.waitFor({ state: 'visible' });
    await menuManager.homePageSwitch.click();

    await Promise.all([waitForMenuResponse(page, 'PUT'), menuManager.saveButton.click()]);
    await page.waitForLoadState('networkidle');

    // API: verify home page was set
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(`"isHomePage":true`);
  });

  test('select a module page from dropdown', async ({ page, request }) => {
    const label = faker.commerce.productName();

    const [response] = await Promise.all([
      waitForMenuResponse(page, 'POST'),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(response);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);

    await menuManager.pageRadio.click();
    await menuManager.pageSelect.waitFor({ state: 'visible' });
    await menuManager.pageSelect.click();
    await page.getByRole('option').first().click();

    await Promise.all([waitForMenuResponse(page, 'PUT'), menuManager.saveButton.click()]);
    await page.waitForLoadState('networkidle');

    // UI: verify item appears
    await expect(menuManager.treeItemButton(label)).toBeVisible();

    // API: verify the page route was saved
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(label);
    expect(flat).toContain('"pageRoute"');
  });
});
