import { expect, test } from '../../fixtures/base';

test.describe('Map read-only API', () => {
  test('GET /api/map/sources returns an array', async ({ request }) => {
    const res = await request.get('/api/map/sources');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(Array.isArray(body)).toBeTruthy();
  });

  test('GET /api/map/maps returns an array', async ({ request }) => {
    const res = await request.get('/api/map/maps');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(Array.isArray(body)).toBeTruthy();
  });

  test('GET /api/map/basemaps returns an array', async ({ request }) => {
    const res = await request.get('/api/map/basemaps');
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    expect(Array.isArray(body)).toBeTruthy();
  });
});
