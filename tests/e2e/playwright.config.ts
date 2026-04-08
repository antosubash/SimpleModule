import { defineConfig, devices } from '@playwright/test';

const isCI = !!process.env.CI;
const baseURL = isCI ? 'http://localhost:5000' : 'https://localhost:5001';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: isCI,
  retries: 0,
  workers: isCI ? 1 : undefined,
  timeout: 15_000,
  expect: { timeout: 3_000 },
  reporter: [['html', {}], ...(isCI ? [['github', {}] as const] : [])],
  use: {
    baseURL,
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
  ],
  webServer: {
    command: isCI
      ? 'dotnet run --project ../../template/SimpleModule.Host --launch-profile http --no-build'
      : 'dotnet run --project ../../template/SimpleModule.Host',
    url: `${baseURL}/health/live`,
    reuseExistingServer: true,
    ignoreHTTPSErrors: true,
    timeout: 120_000,
    env: {
      ...process.env,
      ASPNETCORE_URLS: baseURL,
      Database__DefaultConnection: 'Data Source=e2e-test.db',
      // Process background jobs in-host so e2e tests don't need a separate Worker.
      BackgroundJobs__WorkerMode: 'Consumer',
    },
  },
});
