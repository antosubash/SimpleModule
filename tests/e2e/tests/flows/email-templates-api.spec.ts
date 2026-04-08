import { expect, test } from '../../fixtures/base';

test.describe('Email templates API', () => {
  test('paged list endpoint returns defaults when no query params are provided', async ({
    request,
  }) => {
    const res = await request.get('/api/email/templates');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body).toHaveProperty('items');
    expect(Array.isArray(body.items)).toBeTruthy();
    expect(body.page).toBeGreaterThanOrEqual(1);
    expect(body.pageSize).toBeGreaterThanOrEqual(1);
  });

  test('paged list endpoint respects explicit page/pageSize', async ({ request }) => {
    const res = await request.get('/api/email/templates?page=1&pageSize=5');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body.page).toBe(1);
    expect(body.pageSize).toBe(5);
  });

  test('history endpoint returns defaults when no query params are provided', async ({
    request,
  }) => {
    const res = await request.get('/api/email/messages');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body).toHaveProperty('items');
    expect(Array.isArray(body.items)).toBeTruthy();
  });
});
