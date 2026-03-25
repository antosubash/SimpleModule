import { expect, test } from '../../fixtures/base';
import { MenuManagerPage } from '../../pages/settings/menu-manager.page';

const suffix = Date.now();

test.describe('Menu Manager - CRUD Flows', () => {
  let menuManager: MenuManagerPage;

  test.beforeEach(async ({ page }) => {
    menuManager = new MenuManagerPage(page);
    await menuManager.goto();
    await expect(menuManager.heading).toBeVisible();
  });

  test('create a top-level menu item', async ({ page }) => {
    const label = `Products ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);

    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/products/browse');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await expect(menuManager.treeItemButton(label)).toBeVisible();
  });

  test('create and edit a menu item', async ({ page }) => {
    const label = `About ${suffix}`;
    const editedLabel = `About Us ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/about');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select the item to edit
    await menuManager.selectItem(label);
    await menuManager.labelInput.waitFor({ state: 'visible' });

    // Edit label and URL
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(editedLabel);
    await menuManager.urlInput.clear();
    await menuManager.urlInput.fill('/about-us');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await expect(menuManager.treeItemButton(editedLabel)).toBeVisible();
  });

  test('create a child menu item', async ({ page }) => {
    const parentLabel = `Services ${suffix}`;
    const childLabel = `Consulting ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(parentLabel);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/services');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select parent
    await menuManager.selectItem(parentLabel);

    // Add child
    await menuManager.addChildButton.waitFor({ state: 'visible' });
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addChildButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(childLabel);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/services/consulting');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await expect(menuManager.treeItemButton(childLabel)).toBeVisible();
  });

  test('delete a menu item', async ({ page }) => {
    const label = `ToDelete ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/to-delete');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select and delete
    await menuManager.selectItem(label);
    await menuManager.deleteButton.waitFor({ state: 'visible' });

    // Click Delete to open confirmation dialog
    await menuManager.deleteButton.click();

    // Confirm deletion in the Radix dialog
    const dialog = page.getByRole('alertdialog').or(page.getByRole('dialog'));
    await dialog.getByRole('button', { name: 'Delete' }).click();
    await page.waitForLoadState('networkidle');

    await expect(menuManager.treeItemButton(label)).not.toBeVisible();
  });

  test('toggle visibility of a menu item', async ({ page }) => {
    const label = `ToggleMe ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/toggle');

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
  });

  test('set home page on a menu item', async ({ page }) => {
    const label = `HomePage ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);
    await menuManager.urlRadio.click();
    await menuManager.urlInput.fill('/home-test');

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
  });

  test('select a module page from dropdown', async ({ page }) => {
    const label = `BrowseProducts ${suffix}`;

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill(label);

    // Select "Page" radio
    await menuManager.pageRadio.click();

    // Open the Radix Select and pick first option
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

    await expect(menuManager.treeItemButton(label)).toBeVisible();
  });
});
