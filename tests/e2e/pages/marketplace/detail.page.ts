import type { Page } from '@playwright/test';

export class MarketplaceDetailPage {
  constructor(private page: Page) {}

  get heading() {
    return this.page.getByRole('heading').first();
  }

  get backButton() {
    return this.page
      .getByRole('link', { name: /back to marketplace/i })
      .or(this.page.getByRole('button', { name: /back/i }));
  }

  get smCliTab() {
    return this.page.getByRole('tab', { name: /sm cli/i }).or(this.page.getByText(/sm cli/i));
  }

  get dotnetCliTab() {
    return this.page.getByRole('tab', { name: /\.net cli/i }).or(this.page.getByText(/\.net cli/i));
  }

  get copyButton() {
    return this.page.getByRole('button', { name: /copy/i }).first();
  }

  get readmeSection() {
    return this.page.locator('article, [class*="readme"], [class*="markdown"]');
  }

  get versionsSection() {
    return this.page.getByText(/versions/i);
  }

  get tagsSection() {
    return this.page.getByText(/tags/i);
  }
}
