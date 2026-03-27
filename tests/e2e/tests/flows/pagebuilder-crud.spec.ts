import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { PageBuilderEditorPage } from '../../pages/pagebuilder/editor.page';
import { PageBuilderManagePage } from '../../pages/pagebuilder/manage.page';
import { PageBuilderPagesListPage } from '../../pages/pagebuilder/pages-list.page';
import { PageBuilderViewerPage } from '../../pages/pagebuilder/viewer.page';

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
      await page.waitForLoadState('networkidle');

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
      await page.waitForLoadState('networkidle');

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

test.describe('PageBuilder API auth', () => {
  test('unauthenticated GET returns non-500', async ({ request }) => {
    const res = await request.fetch('/api/pagebuilder', {
      headers: { cookie: '' },
    });
    expect(res.status()).toBeLessThan(500);
  });

  test('GET /api/pagebuilder returns page list', async ({ request }) => {
    const res = await request.get('/api/pagebuilder');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(Array.isArray(body)).toBeTruthy();
  });

  test('POST /api/pagebuilder with empty title returns 400', async ({ request }) => {
    const res = await request.post('/api/pagebuilder', {
      data: { title: '', slug: '' },
    });
    expect(res.status()).toBe(400);
  });
});

test.describe('PageBuilder editor navigation', () => {
  test('new page editor opens and back button returns to manage', async ({ page }) => {
    const manage = new PageBuilderManagePage(page);
    await manage.goto();

    // UI: click New Page
    await manage.newPageButton.click();
    await page.waitForURL('**/pages/new');

    // Dismiss template picker if it appears
    const blankBtn = page.getByRole('button', { name: /blank page/i });
    if (await blankBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
      await blankBtn.click();
    }

    // UI: verify editor loaded
    await expect(page.getByTestId('puck-editor')).toBeVisible({ timeout: 10000 });

    // UI: click back button
    await page.getByRole('button', { name: /back to pages/i }).click();
    await page.waitForURL('**/pages/manage');
  });

  test('edit navigates to editor for existing page', async ({ page, request }) => {
    const title = faker.lorem.words(3);

    // Setup: create a page via API for the test
    const createRes = await request.post('/api/pagebuilder', { data: { title } });
    const created = await createRes.json();

    // UI: navigate to editor
    await page.goto(`/pages/${created.id}/edit`);
    await expect(page.getByTestId('puck-editor')).toBeVisible({ timeout: 10000 });

    // Cleanup
    await request.delete(`/api/pagebuilder/${created.id}`);
  });
});

test.describe('PageBuilder slug generation', () => {
  test('auto-generates slug from title when not provided', async ({ request }) => {
    const title = faker.lorem.words(3);
    const res = await request.post('/api/pagebuilder', { data: { title } });
    expect(res.ok()).toBeTruthy();
    const page = await res.json();
    expect(page.slug).toBeTruthy();
    await request.delete(`/api/pagebuilder/${page.id}`);
  });

  test('duplicate titles get unique slugs', async ({ request }) => {
    const title = faker.lorem.words(3);
    const res1 = await request.post('/api/pagebuilder', { data: { title } });
    const page1 = await res1.json();
    const res2 = await request.post('/api/pagebuilder', { data: { title } });
    const page2 = await res2.json();
    expect(page1.slug).not.toBe(page2.slug);
    expect(page2.slug).toMatch(/-1$/);
    await request.delete(`/api/pagebuilder/${page1.id}`);
    await request.delete(`/api/pagebuilder/${page2.id}`);
  });
});

test.describe
  .serial('PageBuilder Draft Workflow', () => {
    const title = faker.lorem.words(3);
    const slug = faker.helpers.slugify(title).toLowerCase();
    let pageId: number;

    test('create page and save draft content via editor', async ({ page, request }) => {
      // Setup: create page via API (this suite tests the draft workflow, not creation)
      const createRes = await request.post('/api/pagebuilder', { data: { title, slug } });
      const created = await createRes.json();
      pageId = created.id;

      // UI: open editor and save draft
      const editor = new PageBuilderEditorPage(page);
      await editor.gotoEdit(pageId);
      await expect(editor.editorOverlay).toBeVisible({ timeout: 10000 });
      await editor.saveDraft();

      // API: verify draft was saved
      const apiRes = await request.get(`/api/pagebuilder/${pageId}`);
      const apiPage = await apiRes.json();
      expect(apiPage.draftContent).toBeTruthy();
      expect(apiPage.content).toBe('{}');
    });

    test('publish copies draft to live content', async ({ request }) => {
      const publishRes = await request.post(`/api/pagebuilder/${pageId}/publish`);
      expect(publishRes.ok()).toBeTruthy();
      const published = await publishRes.json();
      expect(published.content).not.toBe('{}');
      expect(published.draftContent).toBeNull();
    });

    test('published page renders content', async ({ page, request }) => {
      // API: verify content is published
      const apiRes = await request.get(`/api/pagebuilder/${pageId}`);
      expect(apiRes.ok()).toBeTruthy();
      const apiPage = await apiRes.json();
      expect(apiPage.isPublished).toBe(true);
      expect(apiPage.content).not.toBe('{}');

      // UI: navigate to viewer and verify component mounts
      const viewer = new PageBuilderViewerPage(page);
      await viewer.goto(slug);
      await expect(viewer.content).toBeAttached();
    });

    test('cleanup', async ({ request }) => {
      await request.delete(`/api/pagebuilder/${pageId}`);
    });
  });

test.describe
  .serial('PageBuilder Soft Delete', () => {
    const title = faker.lorem.words(3);
    let pageId: number;

    test('create and soft delete a page', async ({ request }) => {
      const createRes = await request.post('/api/pagebuilder', { data: { title } });
      const created = await createRes.json();
      pageId = created.id;
      const deleteRes = await request.delete(`/api/pagebuilder/${pageId}`);
      expect(deleteRes.status()).toBe(204);
    });

    test('deleted page appears in trash', async ({ request }) => {
      const trashRes = await request.get('/api/pagebuilder/trash');
      expect(trashRes.ok()).toBeTruthy();
      const trashed = await trashRes.json();
      expect(trashed.some((p: { id: number }) => p.id === pageId)).toBeTruthy();
    });

    test('restore page from trash', async ({ request }) => {
      const restoreRes = await request.post(`/api/pagebuilder/${pageId}/restore`);
      expect(restoreRes.ok()).toBeTruthy();
      const listRes = await request.get('/api/pagebuilder');
      const pages = await listRes.json();
      expect(pages.some((p: { id: number }) => p.id === pageId)).toBeTruthy();
    });

    test('permanent delete removes completely', async ({ request }) => {
      await request.delete(`/api/pagebuilder/${pageId}`);
      const permRes = await request.delete(`/api/pagebuilder/${pageId}/permanent`);
      expect(permRes.status()).toBe(204);
      const trashRes = await request.get('/api/pagebuilder/trash');
      const trashed = await trashRes.json();
      expect(trashed.some((p: { id: number }) => p.id === pageId)).toBeFalsy();
    });
  });

test.describe
  .serial('PageBuilder Templates API', () => {
    let templateId: number;

    test('create template', async ({ request }) => {
      const res = await request.post('/api/pagebuilder/templates', {
        data: {
          name: faker.lorem.words(2),
          content: JSON.stringify({
            content: [{ type: 'Hero', props: { id: 'hero-1' } }],
            root: {},
          }),
        },
      });
      expect(res.status()).toBe(201);
      const template = await res.json();
      templateId = template.id;
      expect(template.name).toBeTruthy();
    });

    test('list templates', async ({ request }) => {
      const res = await request.get('/api/pagebuilder/templates');
      expect(res.ok()).toBeTruthy();
      const templates = await res.json();
      expect(Array.isArray(templates)).toBeTruthy();
      expect(templates.some((t: { id: number }) => t.id === templateId)).toBeTruthy();
    });

    test('delete template', async ({ request }) => {
      const res = await request.delete(`/api/pagebuilder/templates/${templateId}`);
      expect(res.status()).toBe(204);
    });
  });

test.describe
  .serial('PageBuilder Tags API', () => {
    let pageId: number;
    const tagName = faker.helpers.slugify(faker.word.noun()).toLowerCase();

    test('add tag to page', async ({ request }) => {
      const createRes = await request.post('/api/pagebuilder', {
        data: { title: faker.lorem.words(2) },
      });
      const page = await createRes.json();
      pageId = page.id;
      const tagRes = await request.post(`/api/pagebuilder/${pageId}/tags`, {
        data: { name: tagName },
      });
      expect(tagRes.status()).toBe(204);
    });

    test('list tags', async ({ request }) => {
      const res = await request.get('/api/pagebuilder/tags');
      expect(res.ok()).toBeTruthy();
      const tags = await res.json();
      expect(tags.some((t: { name: string }) => t.name === tagName)).toBeTruthy();
    });

    test('cleanup', async ({ request }) => {
      await request.delete(`/api/pagebuilder/${pageId}`);
    });
  });

test.describe('PageBuilder Slug Validation', () => {
  test('invalid slug returns 400', async ({ request }) => {
    const res = await request.post('/api/pagebuilder', { data: { title: 'x', slug: 'ab' } });
    expect(res.status()).toBe(400);
  });

  test('valid custom slug is accepted', async ({ request }) => {
    const slug = faker.helpers.slugify(faker.lorem.words(3)).toLowerCase();
    const res = await request.post('/api/pagebuilder', {
      data: { title: faker.lorem.words(2), slug },
    });
    expect(res.ok()).toBeTruthy();
    const page = await res.json();
    expect(page.slug).toBe(slug);
    await request.delete(`/api/pagebuilder/${page.id}`);
  });
});
