import { faker } from '@faker-js/faker';
import type { Page } from '@playwright/test';
import type { MenuManagerPage } from '../../../pages/settings/menu-manager.page';

export const createdIds: number[] = [];

export function trackCreated(resp: {
  url(): string;
  request(): { method(): string };
  json(): Promise<unknown>;
}) {
  if (resp.url().includes('/api/settings/menus') && resp.request().method() === 'POST') {
    resp
      .json()
      .then((body: unknown) => {
        const data = body as Record<string, unknown> | null;
        if (data?.id) createdIds.push(data.id as number);
      })
      .catch(() => {});
  }
}

export async function waitForMenuResponse(
  page: Page,
  method: 'POST' | 'PUT' | 'DELETE' | 'DELETE_OR_PUT',
) {
  return page.waitForResponse((resp) => {
    if (!resp.url().includes('/api/settings/menus')) return false;
    const m = resp.request().method();
    if (method === 'DELETE_OR_PUT') return m === 'DELETE' || m === 'PUT';
    return m === method;
  });
}

export async function addTopLevelItem(
  page: Page,
  menuManager: MenuManagerPage,
  label: string,
  url?: string,
) {
  const [response] = await Promise.all([
    waitForMenuResponse(page, 'POST'),
    menuManager.addItemButton.click(),
  ]);
  trackCreated(response);

  await menuManager.labelInput.waitFor({ state: 'visible' });
  await menuManager.labelInput.clear();
  await menuManager.labelInput.fill(label);
  await menuManager.urlRadio.click();
  await menuManager.urlInput.fill(url ?? `/${faker.helpers.slugify(label).toLowerCase()}`);

  await Promise.all([waitForMenuResponse(page, 'PUT'), menuManager.saveButton.click()]);
  await page.waitForLoadState('networkidle');
}
