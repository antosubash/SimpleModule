import { defineConfig, devices } from '@playwright/test';

/**
 * Root-level config that delegates to tests/e2e.
 * Allows `npx playwright test` from repo root to work correctly.
 */
export default defineConfig({
  testDir: './tests/e2e/tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: [['html'], ...(process.env.CI ? [['github' as const]] : [])],
  use: {
    baseURL: 'https://localhost:5001',
    trace: 'on-first-retry',
    ignoreHTTPSErrors: true,
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'setup',
      testMatch: /.*\.setup\.ts/,
    },
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
      dependencies: ['setup'],
    },
    ...(process.env.CI
      ? [
          {
            name: 'firefox',
            use: { ...devices['Desktop Firefox'] },
            dependencies: ['setup'],
          },
          {
            name: 'webkit',
            use: { ...devices['Desktop Safari'] },
            dependencies: ['setup'],
          },
        ]
      : []),
  ],
  webServer: {
    command: 'dotnet run --project template/SimpleModule.Host',
    url: 'https://localhost:5001/health/live',
    reuseExistingServer: true,
    ignoreHTTPSErrors: true,
    timeout: 60_000,
    env: {
      ...process.env,
      ASPNETCORE_URLS: 'https://localhost:5001',
      Database__DefaultConnection: 'Data Source=e2e-test.db',
    },
  },
});
