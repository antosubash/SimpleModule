import type { Page } from '@playwright/test';

export class ChatBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/chat');
  }

  get heading() {
    return this.page.getByRole('heading', { name: /^chat$/i });
  }

  get startNewConversationSection() {
    return this.page.getByText(/start a new conversation/i);
  }

  get agentSelect() {
    return this.page.locator('select');
  }

  get titleInput() {
    return this.page.getByPlaceholder(/optional title/i);
  }

  get newChatButton() {
    return this.page.getByRole('button', { name: /new chat|creating/i });
  }

  conversationByTitle(title: string) {
    return this.page.getByText(title, { exact: true });
  }
}
