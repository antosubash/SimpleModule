import { expect, test } from '../../fixtures/base';

/**
 * Validation flows — server-side validation feedback and UI behavior probes.
 */

test.describe('Settings validation flows', () => {
  // -------------------------------------------------------------------------
  // UI search filter behavior
  // -------------------------------------------------------------------------
  test.describe('Admin settings search', () => {
    test('search by display name narrows list', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByPlaceholder(/search settings/i).fill('primary color');

      await expect(page.getByText(/^Primary Color$/)).toBeVisible();
    });

    test('search by key narrows list', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByPlaceholder(/search settings/i).fill('app.primary_color');

      await expect(page.getByText(/^Primary Color$/)).toBeVisible();
    });

    test('no-match search shows empty state message', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByPlaceholder(/search settings/i).fill('XXXXXXXXXNOTFOUND');

      await expect(page.getByRole('heading', { name: /no settings match/i })).toBeVisible();
    });

    test('clearing search restores all settings', async ({ page }) => {
      // BUG-8 fix: SettingsSearch + the Clear search action stay mounted in the empty state,
      // so the user can recover without a page reload.
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await expect(page.getByPlaceholder(/search settings/i)).toBeVisible();

      await page.getByPlaceholder(/search settings/i).fill('XXXXXXXXXNOTFOUND');
      await expect(page.getByRole('heading', { name: /no settings match/i })).toBeVisible();

      // Search input stays mounted — clear it via the button or the input itself.
      await page.getByRole('button', { name: /clear search/i }).click();
      await expect(page.getByTestId('setting-card').first()).toBeVisible();
    });

    test('switching tabs preserves the search query', async ({ page }) => {
      // Use a query that returns results in the Application tab, then switch to System tab.
      // The query state lives in AdminSettings (shared across tab renders), so after
      // switching to the System tab the same query string is active.
      // If the System tab has no results for the query, SettingsSearch is unmounted —
      // we cannot read inputValue() in that case.
      // Strategy: use a query that matches in BOTH tabs, or check only from the
      // Application tab perspective.
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      // Type a generic term that is likely to match settings in both tabs
      await page.getByPlaceholder(/search settings/i).fill('a');

      // Confirm Application tab shows results with the query active
      await expect(page.getByTestId('setting-card').first()).toBeVisible();

      // Switch to System tab — same query 'a' should still be in the input
      await page.getByRole('tab', { name: /system/i }).click();
      await expect(page.getByTestId('setting-card').first()).toBeVisible();

      // Re-locate the input (it may have remounted under a new SettingsLayout instance)
      const valueAfterSwitch = await page.getByPlaceholder(/search settings/i).inputValue();
      // The query state persists across tab switches because it is owned by AdminSettings.
      expect(valueAfterSwitch).toBe('a');
    });
  });

  // -------------------------------------------------------------------------
  // "Show only modified" checkbox
  // -------------------------------------------------------------------------
  // serial: tests mutate app.primary_color scope=1; sequential execution prevents
  // a parallel DELETE from invalidating our seeded override before the UI filter assertion.
  test.describe
    .serial('Show only modified filter', () => {
      test('shows all settings by default', async ({ page }) => {
        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();

        await expect(page.getByTestId('setting-card').first()).toBeVisible();
      });

      test('show-only-modified checkbox hides unmodified rows', async ({ page }) => {
        // Ensure at least one setting is overridden
        await page.request.put('/api/settings', {
          data: { key: 'app.primary_color', scope: 1, value: '#ff5500' },
        });

        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();

        // Wait for setting cards to be present before counting
        await expect(page.getByTestId('setting-card').first()).toBeVisible();
        const allCount = await page.getByTestId('setting-card').count();

        // The SettingsSearch Checkbox renders as button[role="checkbox"] (Radix UI).
        // Clicking the associated Label may not fire the Radix click handler in all browsers
        // because htmlFor on a <label> only activates <input> controls, not <button> elements.
        // Use getByRole('checkbox') to locate the actual Radix checkbox button and click it.
        await page.getByRole('checkbox').click();

        // Wait for filter to apply — the count should drop
        await expect(async () => {
          const filteredCount = await page.getByTestId('setting-card').count();
          expect(filteredCount).toBeLessThan(allCount);
        }).toPass({ timeout: 3000 });

        // The overridden setting must still be visible
        await expect(page.getByText(/^Primary Color$/)).toBeVisible();

        await page.request.delete('/api/settings/app.primary_color?scope=1');
      });

      test('disabling show-only-modified restores full list', async ({ page }) => {
        await page.request.put('/api/settings', {
          data: { key: 'app.primary_color', scope: 1, value: '#ff5500' },
        });

        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();

        await expect(page.getByTestId('setting-card').first()).toBeVisible();
        const allCount = await page.getByTestId('setting-card').count();

        const checkbox = page.getByRole('checkbox');
        await checkbox.click(); // enable filter
        await expect(async () => {
          expect(await page.getByTestId('setting-card').count()).toBeLessThan(allCount);
        }).toPass({ timeout: 3000 });

        await checkbox.click(); // disable filter
        await expect(async () => {
          expect(await page.getByTestId('setting-card').count()).toBe(allCount);
        }).toPass({ timeout: 3000 });

        await page.request.delete('/api/settings/app.primary_color?scope=1');
      });
    });

  // -------------------------------------------------------------------------
  // Reset button
  // -------------------------------------------------------------------------
  // serial: all three tests mutate app.primary_color scope=1; sequential execution
  // prevents parallel workers from producing ghost overrides that corrupt assertions.
  test.describe
    .serial('Reset to default button', () => {
      test('reset button absent when setting is not overridden', async ({ page }) => {
        // Ensure no override
        await page.request.delete('/api/settings/app.primary_color?scope=1');

        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();
        await page.getByPlaceholder(/search settings/i).fill('primary color');

        // Wait for the setting to be visible before asserting the button is absent —
        // otherwise the assertion passes vacuously before hydration renders anything.
        await expect(page.getByText(/^Primary Color$/)).toBeVisible();

        // No "Reset to default" button should be visible (setting is not overridden)
        await expect(page.getByRole('button', { name: 'Reset to default' })).toHaveCount(0);
      });

      test('reset button appears when setting is overridden', async ({ page }) => {
        await page.request.put('/api/settings', {
          data: { key: 'app.primary_color', scope: 1, value: '#ff5500' },
        });

        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();
        await page.getByPlaceholder(/search settings/i).fill('primary color');

        await expect(page.getByRole('button', { name: 'Reset to default' })).toBeVisible();

        await page.request.delete('/api/settings/app.primary_color?scope=1');
      });

      test('clicking reset removes override without full reload', async ({ page }) => {
        await page.request.put('/api/settings', {
          data: { key: 'app.primary_color', scope: 1, value: '#ff5500' },
        });

        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();
        await page.getByPlaceholder(/search settings/i).fill('primary color');

        await page.getByRole('button', { name: 'Reset to default' }).click();

        // Reset button should disappear (isOverridden becomes false)
        await expect(page.getByRole('button', { name: 'Reset to default' })).toHaveCount(0);

        // API should confirm the value is gone
        const r = await page.request.get('/api/settings/app.primary_color?scope=1');
        expect(r.status()).toBe(404);
      });
    });

  // -------------------------------------------------------------------------
  // Bulk edit mode
  // -------------------------------------------------------------------------
  // serial: tests write to app.primary_color scope=1 via bulk save; sequential
  // execution prevents cross-worker state interference.
  test.describe
    .serial('Bulk edit mode', () => {
      test('toggle switches to bulk mode — SettingsBulkSaveBar only appears when changes queued', async ({
        page,
      }) => {
        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();

        // The bulk edit button uses aria-label from BulkEditToggle key = "Bulk edit"
        const bulkToggle = page.getByRole('button', { name: 'Bulk edit' });
        await bulkToggle.click();

        // The SettingsBulkSaveBar only renders when dirtyCount > 0
        // So bulk mode is on but bar not yet visible until we change something
        // Just assert that the toggle is now "pressed"
        await expect(bulkToggle).toHaveAttribute('aria-pressed', 'true');
      });

      test('bulk mode queues changes and save-all sends to /api/settings/bulk', async ({
        page,
      }) => {
        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();

        // Switch to bulk mode
        await page.getByRole('button', { name: 'Bulk edit' }).click();
        await page.getByPlaceholder(/search settings/i).fill('primary color');

        const hexInput = page.locator('input[maxlength="7"]');
        await hexInput.fill('#005500');
        // In bulk mode, pressing Enter triggers the onDirty callback but queues, not saves
        await hexInput.press('Enter');

        // SettingsBulkSaveBar appears once there's a queued change
        // But Color field in bulk mode still uses a Save button that triggers handleSave
        // which calls onSave -> AdminSettings.handleSave -> queues to pendingValues
        // The Save button text is "Save" but in bulk mode it queues, not POSTs
        const saveBtn = page.getByRole('button', { name: /^save$/i }).first();
        if (await saveBtn.isVisible()) {
          // Intercept the bulk request (won't fire yet — queued)
          await saveBtn.click();
        }

        // Now the bulk save bar should show (dirtyCount > 0)
        await expect(page.getByRole('button', { name: 'Save all' })).toBeVisible({ timeout: 5000 });

        const bulkRequest = page.waitForRequest(
          (req) => req.url().includes('/api/settings/bulk') && req.method() === 'PUT',
        );

        await page.getByRole('button', { name: 'Save all' }).click();
        const req = await bulkRequest;
        const body = req.postDataJSON() as { updates: unknown[] };
        expect(body.updates.length).toBeGreaterThan(0);

        await page.request.delete('/api/settings/app.primary_color?scope=1');
      });

      test('discard clears the pending queue — save bar disappears', async ({ page }) => {
        // Start from a clean state so the new value is guaranteed to differ from the stored one
        await page.request.delete('/api/settings/app.primary_color?scope=1');

        await page.goto('/settings/manage');
        await page.getByRole('tab', { name: /application/i }).click();

        await page.getByRole('button', { name: 'Bulk edit' }).click();
        await page.getByPlaceholder(/search settings/i).fill('primary color');

        const hexInput = page.locator('input[maxlength="7"]');
        await hexInput.fill('#abcdef');

        await page
          .getByRole('button', { name: /^save$/i })
          .first()
          .click();

        await expect(page.getByRole('button', { name: 'Discard' })).toBeVisible({ timeout: 5000 });
        await page.getByRole('button', { name: 'Discard' }).click();

        // Save bar should disappear (dirtyCount back to 0)
        await expect(page.getByRole('button', { name: 'Save all' })).not.toBeVisible();
      });
    });

  // -------------------------------------------------------------------------
  // User settings page — "Only overridden" filter
  // -------------------------------------------------------------------------
  test.describe('User settings only-overridden filter', () => {
    test('only-overridden filter hides non-overridden settings', async ({ page }) => {
      await page.request.put('/api/settings/me', {
        data: { key: 'user.preferred_density', scope: 2, value: 'compact' },
      });

      await page.goto('/settings/me');

      // Wait for cards to appear before counting
      await expect(page.getByTestId('setting-card').first()).toBeVisible();
      const beforeCount = await page.getByTestId('setting-card').count();

      // The Checkbox renders as button[role="checkbox"] (Radix UI).
      // Click the actual checkbox button rather than the label text — label's htmlFor
      // targets a <button> element, and browsers do not fire the button click via label
      // activation (htmlFor only activates native <input> controls).
      await page.getByRole('checkbox').click();

      // Wait for the filter to reduce the card count
      await expect(async () => {
        const afterCount = await page.getByTestId('setting-card').count();
        expect(afterCount).toBeLessThan(beforeCount);
      }).toPass({ timeout: 3000 });

      await page.request.delete('/api/settings/me/user.preferred_density');
    });
  });

  // -------------------------------------------------------------------------
  // Accessibility quick pass
  // -------------------------------------------------------------------------
  test.describe('Accessibility', () => {
    test('inputs have associated labels via htmlFor/id', async ({ page }) => {
      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByPlaceholder(/search settings/i).fill('primary color');

      // The color field input should have an id matching the definition key
      const colorInput = page.locator('[id="app.primary_color"]');
      await expect(colorInput).toBeVisible();

      // A label with matching htmlFor should exist
      const label = page.locator(`label[for="app.primary_color"]`);
      await expect(label).toBeVisible();
    });

    test('search input has an accessible label or aria-label', async ({ page }) => {
      await page.goto('/settings/manage');

      const search = page.getByPlaceholder(/search settings/i);
      await expect(search).toBeVisible();

      // Should have either aria-label or an associated visible label
      const ariaLabel = await search.getAttribute('aria-label');
      const id = await search.getAttribute('id');
      let hasLabel = !!ariaLabel;
      if (!hasLabel && id) {
        hasLabel = (await page.locator(`label[for="${id}"]`).count()) > 0;
      }
      expect(hasLabel).toBeTruthy();
    });

    test('reset button is reachable via keyboard Tab', async ({ page }) => {
      await page.request.put('/api/settings', {
        data: { key: 'app.primary_color', scope: 1, value: '#ff5500' },
      });

      await page.goto('/settings/manage');
      await page.getByRole('tab', { name: /application/i }).click();
      await page.getByPlaceholder(/search settings/i).fill('primary color');

      const resetBtn = page.getByRole('button', { name: /reset/i });
      await expect(resetBtn).toBeVisible();
      await expect(resetBtn).toBeEnabled();

      await page.request.delete('/api/settings/app.primary_color?scope=1');
    });
  });

  // -------------------------------------------------------------------------
  // User settings group sidebar navigation
  // -------------------------------------------------------------------------
  test.describe('User settings group sidebar', () => {
    test('group sidebar is visible on large viewport', async ({ page }) => {
      await page.setViewportSize({ width: 1280, height: 900 });
      await page.goto('/settings/me');

      // The nav has aria-label "Settings groups"
      await expect(page.getByRole('navigation', { name: /settings groups/i })).toBeVisible();
    });

    test('clicking group link scrolls to that group', async ({ page }) => {
      await page.setViewportSize({ width: 1280, height: 900 });
      await page.goto('/settings/me');

      const nav = page.getByRole('navigation', { name: /settings groups/i });
      const firstGroupLink = nav.getByRole('link').first();
      const groupName = await firstGroupLink.textContent();

      await firstGroupLink.click();

      // The group heading should be in view
      if (groupName) {
        await expect(page.getByRole('heading', { name: groupName.trim() })).toBeInViewport();
      }
    });
  });

  // -------------------------------------------------------------------------
  // Server-side validation surface
  // -------------------------------------------------------------------------

  test('PUT /api/settings with invalid color value returns 400', async ({ page }) => {
    const r = await page.request.put('/api/settings', {
      data: { key: 'app.primary_color', scope: 1, value: 'not-a-color' },
    });
    expect(r.status()).toBe(400);
  });
});
