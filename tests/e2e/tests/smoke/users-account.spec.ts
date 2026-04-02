import { expect, test } from '../../fixtures/base';
import { TwoFactorPage } from '../../pages/users/two-factor.page';

test.describe('Users account pages', () => {
  test('two-factor authentication page loads', async ({ page }) => {
    const twoFactor = new TwoFactorPage(page);
    await twoFactor.goto();
    await expect(twoFactor.heading).toBeVisible();
  });
});
