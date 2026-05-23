import { expect, test } from '../../fixtures/base';

/**
 * Field-type coverage for the 11 SettingTypes.
 *
 * Seeded definitions that cover types: Text, Bool, Color, Email, MultilineText, Select.
 * Types with no seeded definition (Url, Password, DateTime, Number-with-range, Json):
 * those gaps are noted inline — they require either a new definition or API-only testing.
 */

test.describe('Settings field type coverage', () => {
  // -------------------------------------------------------------------------
  // Color field (app.primary_color — type 5, Application scope)
  // -------------------------------------------------------------------------
  test.describe('Color field', () => {
    test('renders color picker and hex text input', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('primary color');

      await expect(page.locator('input[type="color"]')).toBeVisible();
      await expect(page.locator('input[maxlength="7"]')).toBeVisible();
    });

    test('hex text input contains a valid 6-digit hex value', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('primary color');

      const hexInput = page.locator('input[maxlength="7"]');
      await expect(hexInput).toHaveValue(/^#[0-9a-fA-F]{6}$/);
    });

    test('color save persists and round-trips as a plain hex string (not JSON-encoded)', async ({
      page,
    }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('primary color');

      const hexInput = page.locator('input[maxlength="7"]');
      await hexInput.fill('#aabbcc');
      // ColorField shows a "Save" button when hasChanged=true
      await page
        .getByRole('button', { name: /^save$/i })
        .first()
        .click();

      // API must return the decoded plain hex string
      await expect(async () => {
        const r = await page.request.get('/api/settings/app.primary_color?scope=1');
        expect(r.ok()).toBeTruthy();
        const body = (await r.json()) as { value: unknown };
        expect(body.value).toBe('#aabbcc');
      }).toPass({ timeout: 5000 });

      // Cleanup
      await page.request.delete('/api/settings/app.primary_color?scope=1');
    });
  });

  // -------------------------------------------------------------------------
  // Email field (app.support_email — type 7, Application scope)
  // -------------------------------------------------------------------------
  test.describe('Email field', () => {
    test('renders as an email input', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('support email');

      // Email field should render <input type="email"> or a text input with email label
      const emailInput = page
        .locator('[id="app.support_email"]')
        .or(page.locator('input[type="email"]'))
        .first();
      await expect(emailInput).toBeVisible();
    });

    test('accepts a valid email address and saves', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('support email');

      const emailInput = page.locator('[id="app.support_email"]').first();
      await emailInput.fill('support@example.com');
      await emailInput.press('Enter');

      await expect(async () => {
        const r = await page.request.get('/api/settings/app.support_email?scope=1');
        expect(r.ok()).toBeTruthy();
        const body = (await r.json()) as { value: unknown };
        expect(body.value).toBe('support@example.com');
      }).toPass({ timeout: 5000 });

      await page.request.delete('/api/settings/app.support_email?scope=1');
    });
  });

  // -------------------------------------------------------------------------
  // MultilineText field (app.welcome_message — type 9, Application scope)
  // -------------------------------------------------------------------------
  test.describe('MultilineText field', () => {
    test('renders a textarea element', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('welcome message');

      await expect(page.locator('textarea')).toBeVisible();
    });

    test('accepts multiline text and saves', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('welcome message');

      const textarea = page.locator('textarea').first();
      await textarea.fill('Line one\nLine two\nLine three');

      // MultilineText fields use a Save button, not Enter
      const saveBtn = page.getByRole('button', { name: /^save$/i }).first();
      await saveBtn.click();

      await expect(async () => {
        const r = await page.request.get('/api/settings/app.welcome_message?scope=1');
        expect(r.ok()).toBeTruthy();
        const body = (await r.json()) as { value: unknown };
        expect(typeof body.value).toBe('string');
        expect((body.value as string).includes('Line two')).toBeTruthy();
      }).toPass({ timeout: 5000 });

      await page.request.delete('/api/settings/app.welcome_message?scope=1');
    });
  });

  // -------------------------------------------------------------------------
  // Select field (user.preferred_density — type 4, User scope)
  // -------------------------------------------------------------------------
  test.describe('Select field', () => {
    test('renders a combobox with allowedValues as options', async ({ page }) => {
      await page.goto('/settings/me');

      const trigger = page.getByRole('combobox', { name: /display density/i });
      await expect(trigger).toBeVisible();
      await trigger.click();

      await expect(page.getByRole('option', { name: 'compact' })).toBeVisible();
      await expect(page.getByRole('option', { name: 'comfortable' })).toBeVisible();
      await expect(page.getByRole('option', { name: 'spacious' })).toBeVisible();
    });

    test('selecting an option saves via /api/settings/me and returns decoded string', async ({
      page,
    }) => {
      await page.goto('/settings/me');

      const trigger = page.getByRole('combobox', { name: /display density/i });
      await trigger.click();
      await page.getByRole('option', { name: 'spacious' }).click();

      await expect(async () => {
        const r = await page.request.get('/api/settings/user.preferred_density/resolved');
        const body = (await r.json()) as { value: unknown };
        expect(body.value).toBe('spacious');
      }).toPass({ timeout: 5000 });

      // Cleanup
      await page.request.delete('/api/settings/me/user.preferred_density');
    });
  });

  // -------------------------------------------------------------------------
  // Bool field (system.maintenance_mode — type 2, System scope)
  // -------------------------------------------------------------------------
  test.describe('Bool field', () => {
    test('renders a toggle or checkbox', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /system/i }).click();
      await page.getByRole('searchbox').fill('maintenance');

      // Bool fields render as a Switch/Toggle (aria role="switch") or checkbox
      const toggle = page.getByRole('switch').or(page.locator('input[type="checkbox"]')).first();
      await expect(toggle).toBeVisible();
    });

    test('API returns decoded boolean true/false, not string', async ({ page }) => {
      const r = await page.request.put('/api/settings', {
        data: { key: 'system.maintenance_mode', scope: 0, value: true },
      });
      expect(r.ok()).toBeTruthy();

      const get = await page.request.get('/api/settings/system.maintenance_mode?scope=0');
      const body = (await get.json()) as { value: unknown };
      expect(typeof body.value).toBe('boolean');
      expect(body.value).toBe(true);

      // Cleanup
      await page.request.delete('/api/settings/system.maintenance_mode?scope=0');
    });
  });

  // -------------------------------------------------------------------------
  // Text field (app.title — type 0, Application scope)
  // -------------------------------------------------------------------------
  test.describe('Text field', () => {
    test('renders a text input', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('application title');

      await expect(page.locator('[id="app.title"]')).toBeVisible();
    });

    test('saves and persists value across page reload', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('application title');

      const input = page.locator('[id="app.title"]');
      await input.fill('QA Test Title');
      await input.press('Enter');

      await expect(async () => {
        const r = await page.request.get('/api/settings/app.title?scope=1');
        const body = (await r.json()) as { value: unknown };
        expect(body.value).toBe('QA Test Title');
      }).toPass({ timeout: 5000 });

      // Reload and verify persistence
      await page.reload();
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByRole('searchbox').fill('application title');
      await expect(page.locator('[id="app.title"]')).toHaveValue('QA Test Title');

      // Cleanup
      await page.request.delete('/api/settings/app.title?scope=1');
    });
  });

  // -------------------------------------------------------------------------
  // Coverage gaps — types with no seeded definition
  // -------------------------------------------------------------------------

  /**
   * GAP: No seeded definitions exist for:
   * - SettingType.Url (6) — no URL-type definition registered in SettingsModule.cs
   * - SettingType.Password (8) — no Password-type definition registered
   * - SettingType.DateTime (10) — no DateTime-type definition registered
   * - SettingType.Number (1) with Min/Max — FileStorage.MaxFileSizeMb exists but has
   *   no Min/Max constraints in its definition, so range validation cannot be exercised
   *   via the UI
   * - SettingType.Json (3) — no Json-type definition registered in the settings module
   *
   * To close these gaps: add sample definitions in SettingsModule.ConfigureSettings()
   * for developer/QA environments, or add them to the integration test factory.
   * API-level round-trip tests for these types exist in SettingsValidationEndpointTests.cs.
   */
  test('COVERAGE NOTE: Url/Password/DateTime/Number-range/Json types have no seeded definitions', async ({
    page,
  }) => {
    const r = await page.request.get('/api/settings/definitions');
    const defs = (await r.json()) as Array<{ type: number; key: string }>;

    const typeNums = new Set(defs.map((d) => d.type));

    // Types 6 (Url), 8 (Password), 10 (DateTime) have no seeded definitions
    const missingTypes = [6, 8, 10].filter((t) => !typeNums.has(t));

    // This assertion documents the gap — it passes vacuously when they're missing
    // and will start failing (usefully) once definitions are added.
    expect(missingTypes).toEqual(
      [6, 8, 10],
      'Url, Password, and DateTime type definitions are not seeded — UI components cannot be exercised',
    );
  });
});
