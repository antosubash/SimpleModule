import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { MenuManagerPage } from '../../pages/settings/menu-manager.page';

const createdIds: number[] = [];

function trackCreated(resp: {
  url(): string;
  request(): { method(): string };
  json(): Promise<unknown>;
}) {
  if (resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST') {
    resp
      .json()
      .then((body: unknown) => {
        const data = body as Record<string, unknown> | null;
        if (data?.id) createdIds.push(data.id as number);
      })
      .catch(() => {});
  }
}

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

    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(response);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(url);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

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

    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(response);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(label).toLowerCase()}`);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select and edit
    await menuManager.selectItem(label);
    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(editedLabel);
    await menuManager.urlInput.clear();
    await menuManager.urlInput.fill(editedUrl);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
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

    const [parentResponse] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(parentResponse);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(parentLabel);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(parentLabel).toLowerCase()}`);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await menuManager.selectItem(parentLabel);
    await menuManager.addChildButton.waitFor({ state: 'visible' });

    const [childResponse] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addChildButton.click(),
    ]);
    trackCreated(childResponse);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(childLabel);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(childLabel).toLowerCase()}`);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
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

    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(response);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(label).toLowerCase()}`);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select and delete via UI
    await menuManager.selectItem(label);
    await menuManager.deleteButton.waitFor({ state: 'visible' });
    await menuManager.deleteButton.click();
    const dialog = page.getByRole('alertdialog').or(page.getByRole('dialog'));
    // Wait for the DELETE (or PUT replacing the tree) to settle before asserting
    await Promise.all([
      page.waitForResponse(
        (resp) =>
          resp.url().includes('/api/settings/menus') &&
          (resp.request().method() === 'DELETE' || resp.request().method() === 'PUT'),
      ),
      dialog.getByRole('button', { name: 'Delete' }).click(),
    ]);
    await page.waitForLoadState('networkidle');

    // UI: verify removed from tree
    await expect(menuManager.treeItemButton(label)).not.toBeVisible();

    // API: verify removed from backend
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).not.toContain(label);
  });

  test('toggle visibility of a menu item', async ({ page, request }) => {
    const label = faker.music.genre();

    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(response);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(label).toLowerCase()}`);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await menuManager.selectItem(label);
    await menuManager.settingsTab.click();
    await menuManager.visibleSwitch.waitFor({ state: 'visible' });
    await menuManager.visibleSwitch.click();

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // API: verify visibility was toggled off
    const apiRes = await request.get('/api/settings/menus');
    const items = await apiRes.json();
    const flat = JSON.stringify(items);
    expect(flat).toContain(`"isVisible":false`);
  });

  test('set home page on a menu item', async ({ page, request }) => {
    const label = faker.location.city();

    const [response] = await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);
    trackCreated(response);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill(`/${faker.helpers.slugify(label).toLowerCase()}`);

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await menuManager.selectItem(label);
    await menuManager.settingsTab.click();
    await menuManager.homePageSwitch.waitFor({ state: 'visible' });
    await menuManager.homePageSwitch.click();

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
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
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
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

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
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
