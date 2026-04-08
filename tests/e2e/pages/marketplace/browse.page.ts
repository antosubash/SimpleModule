import type { Page } from '@playwright/test';

export class MarketplaceBrowsePage {
  constructor(private page: Page) {}

  async goto() {
    await this.page.goto('/marketplace');
  }

  get heading() {
    return this.page.getByRole('heading', { level: 1, name: /marketplace|modules/i });
  }

  get searchInput() {
    return this.page.getByPlaceholder(/search/i);
  }

  get searchButton() {
    return this.page.getByRole('button', { name: /search/i });
  }

  get loadMoreButton() {
    return this.page.getByRole('button', { name: /load more/i });
  }

  get clearFiltersButton() {
    return this.page.getByRole('button', { name: /clear/i });
  }

  categoryButton(category: string) {
    return this.page.getByRole('button', { name: new RegExp(category, 'i') });
  }

  packageCard(title: string) {
    return this.page.getByText(title);
  }

  async search(query: string) {
    await this.searchInput.fill(query);
    await this.searchButton.click();
    await this.page.waitForLoadState('networkidle');
  }
}
