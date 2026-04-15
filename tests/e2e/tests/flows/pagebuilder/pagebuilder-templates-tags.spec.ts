import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';

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
