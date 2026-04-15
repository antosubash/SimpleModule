import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';

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
