import { expect, test } from '../../fixtures/base';
import { ChatBrowsePage } from '../../pages/chat/browse.page';

test.describe('Chat pages', () => {
  test('browse page loads', async ({ page }) => {
    const browse = new ChatBrowsePage(page);
    await browse.goto();
    await expect(browse.heading).toBeVisible();
  });

  test('browse page shows start-new-conversation panel', async ({ page }) => {
    const browse = new ChatBrowsePage(page);
    await browse.goto();
    await expect(browse.startNewConversationSection).toBeVisible();
    await expect(browse.newChatButton).toBeVisible();
  });
});
