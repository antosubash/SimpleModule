import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';
import { EmailTemplatesPage } from '../../pages/email/email.page';

test.describe('Email template CRUD flows', () => {
  test('create, list, update, and delete a template via API', async ({ page, request }) => {
    const name = `e2e-template-${faker.string.alphanumeric(8)}`;
    const slug = faker.helpers.slugify(name).toLowerCase();
    const updatedName = `${name}-updated`;

    // API: create
    const createRes = await request.post('/api/email/templates', {
      data: {
        name,
        slug,
        subject: 'Welcome {{name}}',
        body: '<p>Hello {{name}}</p>',
        isHtml: true,
        defaultReplyTo: null,
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.name).toBe(name);
    expect(created.slug).toBe(slug);

    // API: list and find it
    const listRes = await request.get('/api/email/templates');
    expect(listRes.ok()).toBeTruthy();
    const list = await listRes.json();
    const items = Array.isArray(list) ? list : (list.items ?? []);
    expect(items.some((t: { slug: string }) => t.slug === slug)).toBeTruthy();

    // UI: templates page shows the new template. Use role=cell + exact match
    // because Playwright's getByText is case-insensitive substring, which
    // would otherwise match both Name and Slug cells.
    const templates = new EmailTemplatesPage(page);
    await templates.goto();
    await expect(page.getByRole('cell', { name, exact: true })).toBeVisible();

    // API: update
    const updateRes = await request.put(`/api/email/templates/${created.id}`, {
      data: {
        name: updatedName,
        subject: 'Welcome back {{name}}',
        body: '<p>Hello again {{name}}</p>',
        isHtml: true,
        defaultReplyTo: null,
      },
    });
    expect(updateRes.ok()).toBeTruthy();

    // API: verify update
    const getRes = await request.get(`/api/email/templates/${created.id}`);
    expect(getRes.ok()).toBeTruthy();
    const updated = await getRes.json();
    expect(updated.name).toBe(updatedName);

    // API: delete
    const deleteRes = await request.delete(`/api/email/templates/${created.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // API: verify gone
    const afterRes = await request.get(`/api/email/templates/${created.id}`);
    expect(afterRes.status()).toBe(404);
  });

  test('get email stats returns counters', async ({ request }) => {
    const statsRes = await request.get('/api/email/stats');
    expect(statsRes.ok()).toBeTruthy();
    const stats = await statsRes.json();
    expect(typeof stats).toBe('object');
    expect(stats).not.toBeNull();
  });

  test('list email messages returns paged result', async ({ request }) => {
    const listRes = await request.get('/api/email/messages');
    expect(listRes.ok()).toBeTruthy();
    const body = await listRes.json();
    expect(body).toHaveProperty('items');
    expect(Array.isArray(body.items)).toBe(true);
  });
});
