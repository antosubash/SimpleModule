import { expect, test } from '../../fixtures/base';
import { FeatureFlagsManagePage } from '../../pages/feature-flags/manage.page';

test.describe('Feature Flags CRUD flows', () => {
  test('toggle a feature flag and verify state persists', async ({ page, request }) => {
    const manage = new FeatureFlagsManagePage(page);

    // API: get all flags to find one to toggle
    const flagsRes = await request.get('/api/feature-flags');
    expect(flagsRes.ok()).toBeTruthy();
    const flags = await flagsRes.json();
    expect(flags.length).toBeGreaterThan(0);

    const flag = flags[0];
    const originalState = flag.isEnabled;

    // API: toggle the flag
    const toggleRes = await request.put(`/api/feature-flags/${encodeURIComponent(flag.name)}`, {
      data: { isEnabled: !originalState },
    });
    expect(toggleRes.ok()).toBeTruthy();

    // API: verify the state changed
    const afterToggleRes = await request.get('/api/feature-flags');
    const afterToggle = await afterToggleRes.json();
    const toggled = afterToggle.find((f: { name: string }) => f.name === flag.name);
    expect(toggled.isEnabled).toBe(!originalState);

    // UI: verify the state on the manage page
    await manage.goto();
    await expect(manage.flagRow(flag.name)).toBeVisible();

    // API: restore original state
    await request.put(`/api/feature-flags/${encodeURIComponent(flag.name)}`, {
      data: { isEnabled: originalState },
    });
  });

  test('add and remove an override via API', async ({ request }) => {
    // API: get a flag to work with
    const flagsRes = await request.get('/api/feature-flags');
    const flags = await flagsRes.json();
    expect(flags.length).toBeGreaterThan(0);
    const flag = flags[0];

    // API: add a user override
    const addRes = await request.post(
      `/api/feature-flags/${encodeURIComponent(flag.name)}/overrides`,
      {
        data: {
          overrideType: 0, // User
          overrideValue: 'e2e-test-user',
          isEnabled: true,
        },
      },
    );
    expect(addRes.ok()).toBeTruthy();
    const override = await addRes.json();
    expect(override.flagName).toBe(flag.name);
    expect(override.overrideValue).toBe('e2e-test-user');

    // API: verify override appears in list
    const overridesRes = await request.get(
      `/api/feature-flags/${encodeURIComponent(flag.name)}/overrides`,
    );
    expect(overridesRes.ok()).toBeTruthy();
    const overrides = await overridesRes.json();
    expect(
      overrides.some((o: { overrideValue: string }) => o.overrideValue === 'e2e-test-user'),
    ).toBeTruthy();

    // API: delete the override
    const deleteRes = await request.delete(`/api/feature-flags/overrides/${override.id}`);
    expect(deleteRes.ok()).toBeTruthy();

    // API: verify override is gone
    const afterDeleteRes = await request.get(
      `/api/feature-flags/${encodeURIComponent(flag.name)}/overrides`,
    );
    const afterDelete = await afterDeleteRes.json();
    expect(
      afterDelete.some((o: { overrideValue: string }) => o.overrideValue === 'e2e-test-user'),
    ).toBeFalsy();
  });

  test('check endpoint returns flag state for current user', async ({ request }) => {
    // API: get a flag
    const flagsRes = await request.get('/api/feature-flags');
    const flags = await flagsRes.json();
    expect(flags.length).toBeGreaterThan(0);
    const flag = flags[0];

    // API: check endpoint
    const checkRes = await request.get(`/api/feature-flags/check/${encodeURIComponent(flag.name)}`);
    expect(checkRes.ok()).toBeTruthy();
    const result = await checkRes.json();
    expect(typeof result.isEnabled).toBe('boolean');
  });

  test('overrides dialog opens from manage page', async ({ page, request }) => {
    const manage = new FeatureFlagsManagePage(page);

    // API: get a flag name
    const flagsRes = await request.get('/api/feature-flags');
    const flags = await flagsRes.json();
    expect(flags.length).toBeGreaterThan(0);
    const flagName = flags[0].name;

    // UI: navigate and open overrides dialog
    await manage.goto();
    await manage.overridesButton(flagName).click();
    await expect(manage.overrideDialog).toBeVisible();
    await expect(manage.addOverrideButton).toBeVisible();
  });
});
