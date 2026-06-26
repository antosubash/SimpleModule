import { expect, test } from '../../fixtures/base';
import { BrandingManagePage } from '../../pages/branding/manage.page';

// Branding is a single global configuration, so these tests mutate shared state —
// run them serially and restore defaults at the end.
test.describe.configure({ mode: 'serial' });

const DEFAULTS = {
  appName: 'SimpleModule',
  logoFileId: null,
  faviconFileId: null,
  colorPrimary: '#059669',
  colorPrimaryDark: '#34d399',
  customCss: '',
  topBar: {
    enabled: false,
    message: '',
    backgroundColor: '#059669',
    textColor: '#ffffff',
    links: [],
    dismissible: true,
  },
  footer: { enabled: false, text: '', links: [], showCopyright: true },
};

test.describe('Branding', () => {
  test.afterAll(async ({ request }) => {
    await request.put('/api/branding', { data: DEFAULTS });
  });

  test('updates branding via API and reads it back', async ({ request }) => {
    const put = await request.put('/api/branding', {
      data: {
        ...DEFAULTS,
        appName: 'E2E Brand',
        colorPrimary: '#123456',
        topBar: { ...DEFAULTS.topBar, enabled: true, message: 'E2E top bar' },
        footer: { ...DEFAULTS.footer, enabled: true, text: 'E2E footer' },
      },
    });
    expect(put.ok()).toBeTruthy();

    const get = await request.get('/api/branding');
    expect(get.ok()).toBeTruthy();
    const branding = await get.json();
    expect(branding.appName).toBe('E2E Brand');
    expect(branding.colorPrimary).toBe('#123456');
    expect(branding.topBar.enabled).toBe(true);
    expect(branding.topBar.message).toBe('E2E top bar');
    expect(branding.footer.enabled).toBe(true);
    expect(branding.footer.text).toBe('E2E footer');
  });

  test('applies branding to the document head and title on a full page load', async ({
    request,
    page,
  }) => {
    await request.put('/api/branding', {
      data: { ...DEFAULTS, appName: 'E2E Brand', colorPrimary: '#123456' },
    });

    await page.goto('/');

    // Primary color is injected into <head> server-side (no flash).
    const primary = await page.evaluate(() =>
      getComputedStyle(document.documentElement).getPropertyValue('--color-primary').trim(),
    );
    expect(primary).toBe('#123456');

    // App name reaches the client as the `branding` shared prop and sets the title.
    await expect(page).toHaveTitle('E2E Brand');
  });

  test('renders the admin manage page', async ({ page }) => {
    const branding = new BrandingManagePage(page);
    await branding.goto();

    await expect(branding.heading).toBeVisible();
    await expect(branding.appNameInput).toBeVisible();
    await expect(branding.saveButton).toBeVisible();
    await expect(branding.showTopBarSwitch).toBeVisible();
    await expect(branding.showFooterSwitch).toBeVisible();
  });

  test('serves 404 from the asset endpoint when no logo is configured', async ({ request }) => {
    await request.put('/api/branding', { data: DEFAULTS });

    const res = await request.get('/api/branding/assets/logo');
    expect(res.status()).toBe(404);
  });
});
