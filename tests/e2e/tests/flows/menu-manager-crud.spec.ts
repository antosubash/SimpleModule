import { expect, test } from '../../fixtures/base';
import { MenuManagerPage } from '../../pages/settings/menu-manager.page';

test.describe('Menu Manager - CRUD Flows', () => {
  let menuManager: MenuManagerPage;

  test.beforeEach(async ({ page }) => {
    menuManager = new MenuManagerPage(page);
    await menuManager.goto();
    await expect(menuManager.heading).toBeVisible();
  });

  test('create a top-level menu item', async ({ page }) => {
    // "Add Item" creates a "New Item" via POST and selects it
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    // Editor should appear with the new item selected
    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('Products');

    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/products/browse');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Verify item appears in tree with updated label
    await expect(menuManager.treeItemButton('Products')).toBeVisible();
  });

  test('create and edit a menu item', async ({ page }) => {
    // Create
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('About');
    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/about');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select the item to edit
    await menuManager.selectItem('About');
    await menuManager.labelInput.waitFor({ state: 'visible' });

    // Edit label and URL
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('About Us');
    await menuManager.urlInput.clear();
    await menuManager.urlInput.fill('/about-us');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Verify updated label
    await expect(menuManager.treeItemButton('About Us')).toBeVisible();
  });

  test('create a child menu item', async ({ page }) => {
    // Create parent
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('Services');
    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/services');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select parent
    await menuManager.selectItem('Services');

    // Add child — "Add Child" button should be visible when a top-level item is selected
    await menuManager.addChildButton.waitFor({ state: 'visible' });
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addChildButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('Consulting');
    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/services/consulting');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Verify child appears
    await expect(menuManager.treeItemButton('Consulting')).toBeVisible();
  });

  test('delete a menu item', async ({ page }) => {
    // Create item to delete
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('ToDelete');
    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/to-delete');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select and delete
    await menuManager.selectItem('ToDelete');
    await menuManager.deleteButton.waitFor({ state: 'visible' });

    // Accept the confirmation dialog
    page.on('dialog', (dialog) => dialog.accept());

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'DELETE',
      ),
      menuManager.deleteButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Verify item removed
    await expect(menuManager.treeItemButton('ToDelete')).not.toBeVisible();
  });

  test('toggle visibility of a menu item', async ({ page }) => {
    // Create item
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('ToggleMe');
    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/toggle');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select and toggle visibility off via the editor switch
    await menuManager.selectItem('ToggleMe');
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
    // Create item
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('HomePage');
    await menuManager.urlRadio.check();
    await menuManager.urlInput.fill('/home-test');

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    // Select and set as home page
    await menuManager.selectItem('HomePage');
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
    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST',
      ),
      menuManager.addItemButton.click(),
    ]);

    await menuManager.labelInput.waitFor({ state: 'visible' });
    await menuManager.labelInput.clear();
    await menuManager.labelInput.fill('Browse Products');

    // Select "Page" radio
    await menuManager.pageRadio.check();

    // The page select should be visible and have options
    await menuManager.pageSelect.waitFor({ state: 'visible' });
    const options = await menuManager.pageSelect.locator('option').count();
    expect(options).toBeGreaterThan(1); // At least one page + placeholder

    // Select the first non-empty option
    await menuManager.pageSelect.selectOption({ index: 1 });

    await Promise.all([
      page.waitForResponse(
        (resp) => resp.url().includes('/api/settings/menus') && resp.request().method() === 'PUT',
      ),
      menuManager.saveButton.click(),
    ]);
    await page.waitForLoadState('networkidle');

    await expect(menuManager.treeItemButton('Browse Products')).toBeVisible();
  });
});
