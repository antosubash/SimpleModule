import { expect, test } from '../../fixtures/base';

// One route per module that renders its own React pages, so each case exercises a
// different RCL assembly.
const MODULE_ROUTES = ['/', '/files', '/settings/me', '/admin', '/branding/manage'] as const;

/**
 * The server declares module -> RCL assembly in the page shell, and the client resolves a
 * page bundle from it instead of guessing "SimpleModule.<Module>" and eating a 404 when the
 * guess is wrong (#287). These assertions are deliberately derived from the shell itself
 * rather than a hardcoded route->assembly table, so they check the actual contract: whatever
 * the server declared for this page's module is what the browser requested, once, and it
 * served.
 */
test.describe('Module page bundles', () => {
  for (const route of MODULE_ROUTES) {
    test(`${route} loads its declared bundle in a single request`, async ({ page }) => {
      const bundles: { status: number; url: string }[] = [];
      const failures: string[] = [];

      page.on('response', (res) => {
        if (res.url().includes('.pages.js')) {
          bundles.push({ status: res.status(), url: res.url() });
        }
        if (res.status() >= 400) {
          failures.push(`${res.status()} ${res.url()}`);
        }
      });

      await page.goto(route);
      await expect.poll(() => bundles.length, { timeout: 10_000 }).toBeGreaterThan(0);

      const { module, declaredAssembly } = await page.evaluate(() => {
        const map = JSON.parse(
          document.querySelector('script[data-module-assemblies]')?.textContent ?? '{}',
        );
        const pageData = JSON.parse(
          document.querySelector('script[data-page="app"]')?.textContent ?? '{}',
        );
        const name: string = pageData.component ?? '';
        const mod = name.split('/')[0];
        return { module: mod, declaredAssembly: map[mod] as string | undefined };
      });

      expect(module, 'shell should name the Inertia component').not.toBe('');
      expect(declaredAssembly, `no assembly declared for module "${module}"`).toBeTruthy();

      // Exactly one bundle request: a second would mean the first candidate 404'd and the
      // client fell back to probing, which is the regression this map removes.
      expect(bundles).toHaveLength(1);
      expect(bundles[0].url).toContain(
        `/_content/${declaredAssembly}/${declaredAssembly}.pages.js`,
      );
      expect(bundles[0].status).toBe(200);
      expect(failures, 'no request should fail').toEqual([]);
    });
  }

  test('compiled stylesheet carries the utility classes module pages use', async ({ page }) => {
    await page.goto('/');

    const styles = await page.evaluate(() => {
      const probe = (cls: string, prop: string) => {
        const el = document.createElement('div');
        el.className = cls;
        document.body.appendChild(el);
        const value = getComputedStyle(el).getPropertyValue(prop);
        el.remove();
        return value;
      };
      return {
        flex: probe('flex', 'display'),
        padding: probe('p-4', 'padding'),
        // No module uses this one, so it must stay unstyled — otherwise the assertions
        // above would pass even against a stylesheet containing every possible utility.
        control: probe('bg-lime-700', 'background-color'),
      };
    });

    // If Tailwind's input set misses a source root, these silently vanish (#288).
    expect(styles.flex).toBe('flex');
    expect(styles.padding).toBe('16px');
    expect(styles.control).toBe('rgba(0, 0, 0, 0)');
  });
});
