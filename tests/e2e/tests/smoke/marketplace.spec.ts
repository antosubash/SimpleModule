import { expect, test } from '../../fixtures/base';
import { MarketplaceBrowsePage } from '../../pages/marketplace/browse.page';

test.describe('Marketplace pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new MarketplaceBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });
});
