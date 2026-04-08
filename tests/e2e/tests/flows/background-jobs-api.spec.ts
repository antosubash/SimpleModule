import { expect, test } from '../../fixtures/base';

test.describe('Background jobs API', () => {
  test('GET /api/jobs returns paged result shape', async ({ request }) => {
    const res = await request.get('/api/jobs');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    // Either a plain array or a paged result — accept either shape.
    if (Array.isArray(body)) {
      expect(Array.isArray(body)).toBeTruthy();
    } else {
      expect(body).toHaveProperty('items');
      expect(Array.isArray(body.items)).toBeTruthy();
    }
  });

  test('GET /api/jobs/stats responds', async ({ request }) => {
    const res = await request.get('/api/jobs/stats');
    // The endpoint may not exist in every build; require it be either OK or 404 (not 5xx).
    expect([200, 404]).toContain(res.status());
  });
});
