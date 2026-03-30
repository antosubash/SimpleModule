import { defineConfig, devices } from '@playwright/test';

const isCI = !!process.env.CI;
const baseURL = isCI ? 'http://localhost:5000' : 'https://localhost:5001';

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: isCI,
  retries: isCI ? 2 : 0,
  workers: isCI ? 1 : undefined,
  reporter: [['html'], ...(isCI ? [['github' as const]] : [])],
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
    ...(isCI
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
    command: 'dotnet run --project ../../template/SimpleModule.Host',
    url: `${baseURL}/health/live`,
    reuseExistingServer: true,
    ignoreHTTPSErrors: true,
    timeout: 120_000,
    env: {
      ...process.env,
      ASPNETCORE_URLS: baseURL,
      Database__DefaultConnection: 'Data Source=e2e-test.db',
    },
  },
});
