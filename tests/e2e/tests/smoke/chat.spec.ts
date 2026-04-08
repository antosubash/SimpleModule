import { expect, test } from '../../fixtures/base';

test.describe('Chat pages', () => {
  test('browse page loads', async ({ page }) => {
    await page.goto('/chat');
    await expect(page.getByRole('heading', { name: /chat/i })).toBeVisible();
  });
});
