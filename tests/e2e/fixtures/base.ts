import path from 'node:path';
import { test as base } from '@playwright/test';

const authFile = path.resolve(__dirname, '../auth/.auth/user.json');

export const test = base.extend({
  // biome-ignore lint/correctness/noEmptyPattern: Playwright requires object destructuring for fixture args
  storageState: async ({}, use) => {
    await use(authFile);
  },
});

export { expect } from '@playwright/test';
