import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  testIgnore: 'localization-offline.spec.ts',
  fullyParallel: true,
  use: {
    ...devices['Desktop Chrome'],
    baseURL: 'http://127.0.0.1:4217',
    channel: process.env['PLAYWRIGHT_CHANNEL'],
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: {
    timeout: 120_000,
    command: 'npm exec ng serve -- --host 127.0.0.1 --port 4217',
    url: 'http://127.0.0.1:4217',
    reuseExistingServer: !process.env['CI'],
  },
});
