import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';
import { PageBuilderEditorPage } from '../../../pages/pagebuilder/editor.page';
import { PageBuilderManagePage } from '../../../pages/pagebuilder/manage.page';
import { PageBuilderPagesListPage } from '../../../pages/pagebuilder/pages-list.page';
import { PageBuilderViewerPage } from '../../../pages/pagebuilder/viewer.page';

test.describe
  .serial('PageBuilder CRUD', () => {
    const pageTitle = faker.lorem.words(3);
    let pageId: number;
    let pageSlug: string;

    test.beforeAll(async ({ request }) => {
      // Clean up accumulated test pages so manage page has < 50 items
      const res = await request.get('/api/pagebuilder');
      if (res.ok()) {
        const pages = await res.json();
        const testPatterns =
          /^(untitled-page|draft-e2e|trash-e2e|e2e-page|nav-test|auto-slug|dup-slug|custom-slug|tag-e2e|viewer-test)/i;
        for (const p of pages) {
          if (testPatterns.test(p.slug)) {
            await request.delete(`/api/pagebuilder/${p.id}`).catch(() => {});
            await request.delete(`/api/pagebuilder/${p.id}/permanent`).catch(() => {});
          }
        }
      }
    });

    test.afterAll(async ({ request }) => {
      // Safety cleanup in case test fails mid-way
      if (pageId) {
        await request.delete(`/api/pagebuilder/${pageId}`).catch(() => {});
      }
    });

    test('create a page and verify it exists on manage page', async ({ page, request }) => {
      // Create via API (setup for UI-driven lifecycle tests below)
      const createRes = await request.post('/api/pagebuilder', {
        data: { title: pageTitle },
      });
      expect(createRes.ok()).toBeTruthy();
      const created = await createRes.json();
      pageId = created.id;
      pageSlug = created.slug;
      expect(pageSlug).toBeTruthy();

      // API: verify the page was persisted
      const apiRes = await request.get(`/api/pagebuilder/${pageId}`);
      expect(apiRes.ok()).toBeTruthy();
      expect((await apiRes.json()).title).toBe(pageTitle);

      // UI: verify it appears on the manage page
      const manage = new PageBuilderManagePage(page);
      await manage.goto();
      await manage.showAllRows();
      await expect(manage.pageRowBySlug(pageSlug)).toBeVisible();
    });

    test('unpublished page returns 404 on public viewer', async ({ page }) => {
      // UI: navigate to the page's public URL
      const response = await page.goto(`/pages/view/${pageSlug}`);
      expect(response?.status()).toBe(404);
    });

    test('publish page via manage page and verify on public list', async ({ page, request }) => {
      const manage = new PageBuilderManagePage(page);
      expect(pageId).toBeTruthy();
      expect(pageSlug).toBeTruthy();

      // UI: navigate to manage page, find the page by slug, publish via dropdown
      await manage.goto();
      await manage.showAllRows();
      await manage.clickActionBySlug(pageSlug, /publish/i);
      await page.waitForTimeout(1000);

      // API: verify the page is now published
      const apiAfter = await request.get(`/api/pagebuilder/${pageId}`);
      const apiPage = await apiAfter.json();
      expect(apiPage.isPublished).toBe(true);

      // UI: verify on public pages list
      const pagesList = new PageBuilderPagesListPage(page);
      await pagesList.goto();
      await expect(page.locator(`a[href*="${pageSlug}"]`)).toBeVisible();
    });

    test('published page renders on public viewer', async ({ page, request }) => {
      // UI: navigate to the viewer
      const viewer = new PageBuilderViewerPage(page);
      await viewer.goto(pageSlug);
      await expect(viewer.content).toBeAttached();

      // API: verify the page data
      const apiRes = await request.get(`/api/pagebuilder/${pageId}`);
      expect(apiRes.ok()).toBeTruthy();
      const apiPage = await apiRes.json();
      expect(apiPage.isPublished).toBe(true);
    });

    test('edit page content in the editor', async ({ page, request }) => {
      const editor = new PageBuilderEditorPage(page);

      // UI: navigate to editor for this page
      await editor.gotoEdit(pageId);
      await expect(editor.editorOverlay).toBeVisible({ timeout: 10000 });

      // UI: save draft content
      await editor.saveDraft();

      // API: verify draft content was saved
      const apiRes = await request.get(`/api/pagebuilder/${pageId}`);
      const apiPage = await apiRes.json();
      expect(apiPage.draftContent).toBeTruthy();
    });

    test('unpublish page via manage page and verify removed from public list', async ({
      page,
      request,
    }) => {
      const manage = new PageBuilderManagePage(page);

      // UI: navigate to manage page, unpublish via dropdown
      await manage.goto();
      await manage.showAllRows();
      await manage.clickActionBySlug(pageSlug, /unpublish/i);
      await page.waitForTimeout(1000);

      // API: verify the page is unpublished
      const apiAfter = await request.get(`/api/pagebuilder/${pageId}`);
      const apiPage = await apiAfter.json();
      expect(apiPage.isPublished).toBe(false);

      // UI: verify removed from public pages list
      const pagesList = new PageBuilderPagesListPage(page);
      await pagesList.goto();
      await expect(page.locator(`a[href*="${pageSlug}"]`)).not.toBeVisible();
    });

    test('delete page via manage page and verify removed', async ({ page, request }) => {
      const manage = new PageBuilderManagePage(page);

      // UI: navigate to manage page, delete via dropdown
      await manage.goto();
      await manage.showAllRows();
      await manage.clickActionBySlug(pageSlug, /delete/i);

      // UI: confirm deletion in the dialog
      const dialog = page.getByRole('alertdialog').or(page.getByRole('dialog'));
      await dialog.getByRole('button', { name: /delete/i }).click();
      await page.waitForLoadState('networkidle');

      // API: verify the page is gone
      const apiRes = await request.get('/api/pagebuilder');
      const pages = await apiRes.json();
      expect(pages.some((p: { id: number }) => p.id === pageId)).toBeFalsy();
    });
  });
