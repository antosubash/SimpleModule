import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';
import { PageBuilderEditorPage } from '../../../pages/pagebuilder/editor.page';
import { PageBuilderViewerPage } from '../../../pages/pagebuilder/viewer.page';

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
