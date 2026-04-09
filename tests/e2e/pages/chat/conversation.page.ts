import type { Page } from '@playwright/test';

export class ChatConversationPage {
  constructor(
    private page: Page,
    private conversationId: string,
  ) {}

  async goto() {
    await this.page.goto(`/chat/${this.conversationId}`);
  }

  get messageInput() {
    return this.page.getByPlaceholder(/type a message/i);
  }

  get sendButton() {
    return this.page.getByRole('button', { name: /^send$/i });
  }

  get stopButton() {
    return this.page.getByRole('button', { name: /^stop$/i });
  }
}
