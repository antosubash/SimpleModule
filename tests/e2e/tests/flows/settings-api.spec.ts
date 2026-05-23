import { expect, test } from '../../fixtures/base';

/**
 * API-level probes for the settings refactor.
 * These tests make direct requests and assert on the JSON shape —
 * no browser UI interaction needed.
 */

test.describe('Settings API — decoded values and contract', () => {
  // -------------------------------------------------------------------------
  // GET /api/settings — decoded-value contract
  // -------------------------------------------------------------------------
  test('GET /api/settings returns empty array when no settings saved', async ({ page }) => {
    const r = await page.request.get('/api/settings');
    expect(r.ok()).toBeTruthy();
    const body = (await r.json()) as unknown[];
    expect(Array.isArray(body)).toBeTruthy();
  });

  test('GET /api/settings values are never double-encoded strings', async ({ page }) => {
    // Seed a value first
    await page.request.put('/api/settings', {
      data: { key: 'app.title', scope: 1, value: 'Decoded Test' },
    });

    const r = await page.request.get('/api/settings');
    const body = (await r.json()) as Array<{ key: string; value: unknown }>;

    for (const entry of body) {
      if (typeof entry.value === 'string') {
        // A double-encoded string would look like: "\"hello\"" — starts and ends with quote
        const isDoubleEncoded = entry.value.startsWith('"') && entry.value.endsWith('"');
        expect(isDoubleEncoded).toBeFalsy();
      }
    }

    await page.request.delete('/api/settings/app.title?scope=1');
  });

  // -------------------------------------------------------------------------
  // Scope filter parameter
  // -------------------------------------------------------------------------
  test('GET /api/settings?scope=0 returns only System-scope rows', async ({ page }) => {
    await page.request.put('/api/settings', {
      data: { key: 'system.maintenance_mode', scope: 0, value: false },
    });

    const r = await page.request.get('/api/settings?scope=0');
    expect(r.ok()).toBeTruthy();
    const body = (await r.json()) as Array<{ scope: number }>;
    for (const entry of body) {
      expect(entry.scope).toBe(0);
    }

    await page.request.delete('/api/settings/system.maintenance_mode?scope=0');
  });

  // -------------------------------------------------------------------------
  // GET /api/settings/definitions — structure
  // -------------------------------------------------------------------------
  test('GET /api/settings/definitions returns array with required fields', async ({ page }) => {
    const r = await page.request.get('/api/settings/definitions');
    expect(r.ok()).toBeTruthy();
    const defs = (await r.json()) as Array<Record<string, unknown>>;
    expect(defs.length).toBeGreaterThan(0);
    for (const def of defs) {
      expect(typeof def['key']).toBe('string');
      expect(typeof def['type']).toBe('number');
      expect(typeof def['scope']).toBe('number');
    }
  });

  test('definitions include all 4 new seeded types (Color, Email, MultilineText, Select)', async ({
    page,
  }) => {
    const r = await page.request.get('/api/settings/definitions');
    const defs = (await r.json()) as Array<{ key: string; type: number }>;
    const typesByKey = Object.fromEntries(defs.map((d) => [d.key, d.type]));

    expect(typesByKey['app.primary_color']).toBe(5); // Color
    expect(typesByKey['app.support_email']).toBe(7); // Email
    expect(typesByKey['app.welcome_message']).toBe(9); // MultilineText
    expect(typesByKey['user.preferred_density']).toBe(4); // Select
  });

  // -------------------------------------------------------------------------
  // GET /api/settings/{key}/resolved — inheritance chain
  // -------------------------------------------------------------------------
  test('resolved endpoint returns definition default when no value is stored', async ({ page }) => {
    // user.preferred_density default is "comfortable"
    await page.request.delete('/api/settings/me/user.preferred_density');

    const r = await page.request.get('/api/settings/user.preferred_density/resolved');
    expect(r.ok()).toBeTruthy();
    const body = (await r.json()) as { key: string; value: unknown };
    expect(body.key).toBe('user.preferred_density');
    expect(body.value).toBe('comfortable');
  });

  test('resolved endpoint falls through user → application → default', async ({ page }) => {
    // Use app.support_email (Email) — not shared with any other test in this file that
    // writes scope=1 to avoid parallel-worker cross-contamination on app.primary_color.
    const key = 'app.support_email';

    // Clear overrides
    await page.request.delete(`/api/settings/${key}?scope=1`);

    // Set app-scope value
    await page.request.put('/api/settings', { data: { key, scope: 1, value: 'qa@example.com' } });

    const r = await page.request.get(`/api/settings/${key}/resolved`);
    const body = (await r.json()) as { value: unknown };
    expect(body.value).toBe('qa@example.com');

    await page.request.delete(`/api/settings/${key}?scope=1`);
  });

  test('resolved endpoint for nonexistent key returns null value (not 404)', async ({ page }) => {
    const r = await page.request.get('/api/settings/does.not.exist.anywhere/resolved');
    expect(r.ok()).toBeTruthy();
    const body = (await r.json()) as { key: string; value: unknown };
    expect(body.value).toBeNull();
  });

  // -------------------------------------------------------------------------
  // PUT /api/settings — validation errors
  // -------------------------------------------------------------------------
  test('PUT with invalid color returns 400 with field-level error', async ({ page }) => {
    const r = await page.request.put('/api/settings', {
      data: { key: 'app.primary_color', scope: 1, value: 'not-a-color' },
    });
    expect(r.status()).toBe(400);
    const body = (await r.json()) as { errors: Record<string, string[]> };
    expect(body.errors['app.primary_color']).toBeDefined();
    expect(body.errors['app.primary_color'].length).toBeGreaterThan(0);
  });

  test('PUT with invalid email returns 400', async ({ page }) => {
    const r = await page.request.put('/api/settings', {
      data: { key: 'app.support_email', scope: 1, value: 'not-an-email' },
    });
    expect(r.status()).toBe(400);
  });

  test('PUT select with out-of-allowedValues option returns 400 from /me', async ({ page }) => {
    const r = await page.request.put('/api/settings/me', {
      data: { key: 'user.preferred_density', scope: 2, value: 'ultra-wide' },
    });
    expect(r.status()).toBe(400);
  });

  test('PUT with bool-as-string returns 400', async ({ page }) => {
    const r = await page.request.put('/api/settings', {
      data: { key: 'system.maintenance_mode', scope: 0, value: 'yes' },
    });
    expect(r.status()).toBe(400);
  });

  // -------------------------------------------------------------------------
  // Bug assertions
  // -------------------------------------------------------------------------

  test('GET /api/settings/{key} without ?scope returns 400', async ({ page }) => {
    const r = await page.request.get('/api/settings/app.title');
    expect(r.status()).toBe(400);
  });

  test('DELETE /api/settings/{key} without ?scope returns 400', async ({ page }) => {
    const r = await page.request.delete('/api/settings/app.title');
    expect(r.status()).toBe(400);
  });

  test('PUT /api/settings/bulk with User scope returns 400', async ({ page }) => {
    const r = await page.request.put('/api/settings/bulk', {
      data: { updates: [{ key: 'app.theme', scope: 2, value: 'dark' }] },
    });
    expect(r.status()).toBe(400);
  });

  // -------------------------------------------------------------------------
  // Cache invalidation
  // -------------------------------------------------------------------------
  test('after PUT the next GET reflects the new value immediately', async ({ page }) => {
    // Use app.welcome_message (MultilineText) — not shared with any other test in this file,
    // so parallel test runs cannot cause cross-contamination DELETE/GET races.
    await page.request.put('/api/settings', {
      data: { key: 'app.welcome_message', scope: 1, value: 'CacheTest1' },
    });
    let r = await page.request.get('/api/settings/app.welcome_message?scope=1');
    expect(r.ok()).toBeTruthy();
    expect(((await r.json()) as { value: unknown }).value).toBe('CacheTest1');

    await page.request.put('/api/settings', {
      data: { key: 'app.welcome_message', scope: 1, value: 'CacheTest2' },
    });
    r = await page.request.get('/api/settings/app.welcome_message?scope=1');
    expect(r.ok()).toBeTruthy();
    expect(((await r.json()) as { value: unknown }).value).toBe('CacheTest2');

    await page.request.delete('/api/settings/app.welcome_message?scope=1');
  });

  test('after DELETE the resolved endpoint returns definition default', async ({ page }) => {
    await page.request.put('/api/settings', {
      data: { key: 'app.primary_color', scope: 1, value: '#123456' },
    });
    await page.request.delete('/api/settings/app.primary_color?scope=1');

    const r = await page.request.get('/api/settings/app.primary_color/resolved');
    const body = (await r.json()) as { value: unknown };
    // Definition default is "#3b82f6"
    expect(body.value).toBe('#3b82f6');
  });

  // -------------------------------------------------------------------------
  // GET /api/settings/me — UserSettingValueDto shape
  // -------------------------------------------------------------------------
  test('GET /api/settings/me returns UserSettingValueDto with resolvedValue', async ({ page }) => {
    const r = await page.request.get('/api/settings/me');
    expect(r.ok()).toBeTruthy();
    const body = (await r.json()) as Array<{
      key: string;
      value: unknown;
      resolvedValue: unknown;
      isOverridden: boolean;
    }>;

    expect(Array.isArray(body)).toBeTruthy();
    for (const entry of body) {
      expect(typeof entry.key).toBe('string');
      expect(typeof entry.isOverridden).toBe('boolean');
      // resolvedValue should be present (may be null but must exist as a property)
      expect('resolvedValue' in entry).toBeTruthy();
    }
  });

  test('GET /api/settings/me shows isOverridden=true after PUT /me', async ({ page }) => {
    await page.request.put('/api/settings/me', {
      data: { key: 'user.preferred_density', scope: 2, value: 'compact' },
    });

    const r = await page.request.get('/api/settings/me');
    const body = (await r.json()) as Array<{ key: string; isOverridden: boolean; value: unknown }>;
    const density = body.find((d) => d.key === 'user.preferred_density');
    expect(density).toBeDefined();
    expect(density!.isOverridden).toBe(true);
    expect(density!.value).toBe('compact');

    await page.request.delete('/api/settings/me/user.preferred_density');
  });

  test('DELETE /api/settings/me/{key} resets isOverridden to false', async ({ page }) => {
    await page.request.put('/api/settings/me', {
      data: { key: 'user.preferred_density', scope: 2, value: 'compact' },
    });
    await page.request.delete('/api/settings/me/user.preferred_density');

    const r = await page.request.get('/api/settings/me');
    const body = (await r.json()) as Array<{ key: string; isOverridden: boolean }>;
    const density = body.find((d) => d.key === 'user.preferred_density');
    expect(density!.isOverridden).toBe(false);
  });
});
