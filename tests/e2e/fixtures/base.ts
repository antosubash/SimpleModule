import path from 'node:path';
import { test as base } from '@playwright/test';

const authFile = path.resolve(__dirname, '../auth/.auth/user.json');

export const test = base.extend({
  storageState: async ({}, use) => {
    await use(authFile);
  },
});

export { expect } from '@playwright/test';
