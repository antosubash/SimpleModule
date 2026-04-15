import { faker } from '@faker-js/faker';
import { expect, test } from '../../../fixtures/base';
import { PageBuilderManagePage } from '../../../pages/pagebuilder/manage.page';

test.describe('PageBuilder editor navigation', () => {
  test('new page editor opens and back button returns to manage', async ({ page }) => {
    const manage = new PageBuilderManagePage(page);
    await manage.goto();

    // UI: click New Page
    await manage.newPageButton.click();
    await page.waitForURL('**/pages/new');

    // Dismiss template picker if it appears
    const blankBtn = page.getByRole('button', { name: /blank page/i });
    if (await blankBtn.isVisible({ timeout: 2000 }).catch(() => false)) {
      await blankBtn.click();
    }

    // UI: verify editor loaded
    await expect(page.getByTestId('puck-editor')).toBeVisible({ timeout: 10000 });

    // UI: click back button
    await page.getByRole('button', { name: /back to pages/i }).click();
    await page.waitForURL('**/pages/manage');
  });

  test('edit navigates to editor for existing page', async ({ page, request }) => {
    const title = faker.lorem.words(3);

    // Setup: create a page via API for the test
    const createRes = await request.post('/api/pagebuilder', { data: { title } });
    const created = await createRes.json();

    // UI: navigate to editor
    await page.goto(`/pages/${created.id}/edit`);
    await expect(page.getByTestId('puck-editor')).toBeVisible({ timeout: 10000 });

    // Cleanup
    await request.delete(`/api/pagebuilder/${created.id}`);
  });
});
