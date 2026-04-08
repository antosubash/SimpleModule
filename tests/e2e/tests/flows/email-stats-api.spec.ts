import { expect, test } from '../../fixtures/base';

test.describe('Email stats API', () => {
  test('GET /api/email/stats returns a payload', async ({ request }) => {
    const res = await request.get('/api/email/stats');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(body).toBeTruthy();
  });
});
