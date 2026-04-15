import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';

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
