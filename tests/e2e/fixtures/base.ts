import { test as base } from '@playwright/test';

export const test = base.extend({
  storageState: async ({}, use) => {
    await use('auth/.auth/user.json');
  },
});

export { expect } from '@playwright/test';
