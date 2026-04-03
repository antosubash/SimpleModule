import { expect, test } from '../../fixtures/base';
import { RateLimitingAdminPage } from '../../pages/rate-limiting/admin.page';

test.describe('Rate Limiting CRUD flows', () => {
  const testPolicyName = `e2e-test-${Date.now()}`;

  test('create, verify, and delete a rate limit rule via API', async ({ request }) => {
    // API: create a rule
    const createRes = await request.post('/api/rate-limiting', {
      data: {
        policyName: testPolicyName,
        policyType: 'FixedWindow',
        target: 'Ip',
        permitLimit: 100,
        windowSeconds: 60,
        segmentsPerWindow: 4,
        tokenLimit: 100,
        tokensPerPeriod: 10,
        replenishmentPeriodSeconds: 10,
        queueLimit: 0,
        isEnabled: true,
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();
    expect(created.policyName).toBe(testPolicyName);
    expect(created.permitLimit).toBe(100);

    // API: get all rules and verify our rule is present
    const allRes = await request.get('/api/rate-limiting');
    expect(allRes.ok()).toBeTruthy();
    const rules = await allRes.json();
    expect(rules.some((r: { policyName: string }) => r.policyName === testPolicyName)).toBeTruthy();

    // API: get rule by ID
    const getRes = await request.get(`/api/rate-limiting/${created.id}`);
    expect(getRes.ok()).toBeTruthy();
    const fetched = await getRes.json();
    expect(fetched.policyName).toBe(testPolicyName);

    // API: update the rule
    const updateRes = await request.put(`/api/rate-limiting/${created.id}`, {
      data: {
        policyType: 'SlidingWindow',
        target: 'User',
        permitLimit: 200,
        windowSeconds: 120,
        segmentsPerWindow: 6,
        tokenLimit: 100,
        tokensPerPeriod: 10,
        replenishmentPeriodSeconds: 10,
        queueLimit: 0,
        isEnabled: true,
      },
    });
    expect(updateRes.ok()).toBeTruthy();
    const updated = await updateRes.json();
    expect(updated.permitLimit).toBe(200);
    expect(updated.policyType).toBe('SlidingWindow');
    expect(updated.target).toBe('User');

    // API: delete the rule
    const deleteRes = await request.delete(`/api/rate-limiting/${created.id}`);
    expect(deleteRes.status()).toBe(204);

    // API: verify deletion
    const afterDeleteRes = await request.get(`/api/rate-limiting/${created.id}`);
    expect(afterDeleteRes.status()).toBe(404);
  });

  test('active policies endpoint returns registered policies', async ({ request }) => {
    const res = await request.get('/api/rate-limiting/active');
    expect(res.ok()).toBeTruthy();
    const policies = await res.json();
    expect(policies.length).toBeGreaterThan(0);

    // Verify built-in policies exist
    const names = policies.map((p: { name: string }) => p.name);
    expect(names).toContain('fixed-default');
    expect(names).toContain('sliding-strict');
    expect(names).toContain('token-bucket');
    expect(names).toContain('auth-strict');
  });

  test('admin page shows created rule in UI', async ({ page, request }) => {
    const admin = new RateLimitingAdminPage(page);
    const ruleName = `e2e-ui-${Date.now()}`;

    // API: create a rule
    const createRes = await request.post('/api/rate-limiting', {
      data: {
        policyName: ruleName,
        policyType: 'FixedWindow',
        target: 'Ip',
        permitLimit: 50,
        windowSeconds: 30,
        segmentsPerWindow: 4,
        tokenLimit: 100,
        tokensPerPeriod: 10,
        replenishmentPeriodSeconds: 10,
        queueLimit: 0,
        isEnabled: true,
      },
    });
    expect(createRes.ok()).toBeTruthy();
    const created = await createRes.json();

    // UI: verify the rule appears on the admin page
    await admin.goto();
    await expect(admin.ruleRow(ruleName)).toBeVisible();

    // Cleanup: delete via API
    await request.delete(`/api/rate-limiting/${created.id}`);
  });

  test('create rule dialog opens from admin page', async ({ page }) => {
    const admin = new RateLimitingAdminPage(page);
    await admin.goto();
    await admin.createRuleButton.click();
    await expect(admin.createDialog).toBeVisible();
    await expect(admin.policyNameInput).toBeVisible();
  });
});
