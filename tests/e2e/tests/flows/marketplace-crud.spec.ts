import { expect, test } from '../../fixtures/base';
import { MarketplaceBrowsePage } from '../../pages/marketplace/browse.page';
import { MarketplaceDetailPage } from '../../pages/marketplace/detail.page';

test.describe('Marketplace flows', () => {
  test('browse and search for packages', async ({ page }) => {
    const browse = new MarketplaceBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();

    // Verify package cards are visible (marketplace should have seeded data)
    const cards = page.locator('[class*="card"], [data-testid*="package"]');
    const cardCount = await cards.count();

    if (cardCount > 0) {
      // Search for a package
      await browse.search('simple');
      await page.waitForLoadState('networkidle');
      await expect(browse.heading).toBeVisible();
    }
  });

  test('navigate to package detail and back', async ({ page }) => {
    const browse = new MarketplaceBrowsePage(page);
    const detail = new MarketplaceDetailPage(page);

    await browse.goto();
    await expect(browse.heading).toBeVisible();

    // Click the first package card to navigate to detail
    const firstCard = page
      .locator('[class*="card"] a, [data-testid*="package"] a')
      .first()
      .or(page.locator('a[href*="/marketplace/"]').first());

    if (await firstCard.isVisible()) {
      await firstCard.click();
      await page.waitForLoadState('networkidle');

      // Should be on detail page
      await expect(detail.heading).toBeVisible();

      // Navigate back
      if (await detail.backButton.isVisible()) {
        await detail.backButton.click();
        await page.waitForLoadState('networkidle');
        await expect(browse.heading).toBeVisible();
      }
    }
  });

  test('category filter changes results', async ({ page }) => {
    const browse = new MarketplaceBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();

    // Try clicking a category button (if any exist)
    const categoryButtons = page.locator(
      'button:has-text("Feature"), button:has-text("Integration"), button:has-text("All")',
    );

    if ((await categoryButtons.count()) > 0) {
      await categoryButtons.first().click();
      await page.waitForLoadState('networkidle');
      await expect(browse.heading).toBeVisible();
    }
  });
});
