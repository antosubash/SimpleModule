import { expect, test } from '../../fixtures/base';
import { PageBuilderEditorPage } from '../../pages/pagebuilder/editor.page';
import { PageBuilderManagePage } from '../../pages/pagebuilder/manage.page';
import { PageBuilderPagesListPage } from '../../pages/pagebuilder/pages-list.page';

test.describe('PageBuilder pages', () => {
  test('manage page loads', async ({ page }) => {
    const manage = new PageBuilderManagePage(page);
    await manage.goto();
    await expect(manage.heading).toBeVisible();
  });

  test('new page editor loads with fullscreen overlay', async ({ page }) => {
    const editor = new PageBuilderEditorPage(page);
    await editor.gotoNew();
    await expect(editor.editorOverlay).toBeVisible();
  });

  test('editor has back button', async ({ page }) => {
    const editor = new PageBuilderEditorPage(page);
    await editor.gotoNew();
    await expect(editor.backButton).toBeVisible();
  });

  test('public pages list loads', async ({ page }) => {
    const pagesList = new PageBuilderPagesListPage(page);
    await pagesList.goto();
    await expect(pagesList.heading).toBeVisible();
  });
});
