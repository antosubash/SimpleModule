import { expect, test } from '../../fixtures/base';

test.describe('Health endpoints', () => {
  test('liveness probe returns Healthy', async ({ request }) => {
    const res = await request.get('/health/live');
    expect(res.ok()).toBeTruthy();
    expect(await res.text()).toContain('Healthy');
  });

  test('readiness probe responds', async ({ request }) => {
    const res = await request.get('/health/ready');
    // Ready may be 200 or 503 depending on downstream deps; either is a valid response.
    expect([200, 503]).toContain(res.status());
  });
});
