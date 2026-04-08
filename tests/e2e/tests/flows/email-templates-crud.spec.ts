import { faker } from '@faker-js/faker';
import { expect, test } from '../../fixtures/base';

test.describe('Email templates CRUD', () => {
  test('create, verify, update, delete an email template', async ({ page, request }) => {
    const name = `e2e-tpl-${faker.string.alphanumeric(6)}`;
    const slug = `e2e-tpl-${faker.string.alphanumeric(6).toLowerCase()}`;
    const subject = faker.lorem.sentence(4);
    const body = `<p>${faker.lorem.paragraph()}</p>`;
    const updatedName = `${name}-upd`;
    const updatedSubject = `${subject} (updated)`;

    // Create via API
    const createRes = await request.post('/api/email/templates', {
      data: { name, slug, subject, body, isHtml: true, defaultReplyTo: null },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.id).toBeTruthy();

    // API: list should include it
    const listRes = await request.get('/api/email/templates');
    expect(listRes.ok()).toBeTruthy();
    const listBody = await listRes.json();
    const items = Array.isArray(listBody) ? listBody : (listBody.items ?? []);
    expect(items.find((t: { id: number }) => t.id === created.id)).toBeTruthy();

    // UI: templates page shows the new template
    await page.goto('/email/templates');
    await expect(page.getByText(name)).toBeVisible();

    // Update via API (PUT)
    const updateRes = await request.put(`/api/email/templates/${created.id}`, {
      data: {
        name: updatedName,
        subject: updatedSubject,
        body,
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
    expect(updated.subject).toBe(updatedSubject);

    // Delete via API
    const deleteRes = await request.delete(`/api/email/templates/${created.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // UI: gone from templates page
    await page.goto('/email/templates');
    await expect(page.getByText(updatedName)).not.toBeVisible();
  });
});
