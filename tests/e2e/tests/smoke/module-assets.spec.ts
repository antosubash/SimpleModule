import { expect, test } from '../../fixtures/base';

// Manifest-driven module loading: the host injects a module → bundle map built
// from each module assembly's compile-time manifest, and the client resolves
// page bundles through it (docs/site/advanced/module-packaging.md).
test.describe('Module asset manifest', () => {
  test('HTML shell injects the sm-module-assets map', async ({ page }) => {
    await page.goto('/');

    const scripts = page.locator('script#sm-module-assets');
    await expect(scripts).toHaveCount(1);
    await expect(scripts).toHaveAttribute('type', 'application/json');

    const map = JSON.parse((await scripts.textContent()) ?? '{}') as Record<string, string>;
    expect(Object.keys(map).length).toBeGreaterThan(0);
    expect(map.FeatureFlags).toBe(
      '_content/SimpleModule.FeatureFlags/SimpleModule.FeatureFlags.pages.js',
    );
    for (const entry of Object.values(map)) {
      expect(entry).toMatch(/^_content\/.+\.pages\.js$/);
    }
  });

  test('module bundles declared in the map are served', async ({ page }) => {
    await page.goto('/');
    const scripts = page.locator('script#sm-module-assets');
    const map = JSON.parse((await scripts.textContent()) ?? '{}') as Record<string, string>;

    const entry = map.FeatureFlags;
    expect(entry).toBeTruthy();
    const response = await page.request.get(`/${entry}`);
    expect(response.ok()).toBeTruthy();
    expect(await response.text()).toContain('pages');
  });

  test('module page renders via the manifest map without console errors', async ({ page }) => {
    const errors: string[] = [];
    page.on('console', (m) => m.type() === 'error' && errors.push(m.text()));
    page.on('pageerror', (e) => errors.push(e.message));

    await page.goto('/feature-flags/manage');
    await expect(page.getByRole('heading', { name: /feature flags/i })).toBeVisible();
    await page.waitForLoadState('networkidle');

    expect(errors).toEqual([]);
  });
});
