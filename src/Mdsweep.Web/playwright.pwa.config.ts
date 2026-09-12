import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  outputDir: './test-results-pwa',
  testMatch: 'localization-offline.spec.ts',
  timeout: 90_000,
  use: {
    ...devices['Desktop Chrome'],
    baseURL: 'http://127.0.0.1:4218',
    screenshot: 'only-on-failure',
    trace: 'retain-on-failure',
  },
  webServer: {
    command: 'node scripts/serve-pwa-test.mjs',
    url: 'http://127.0.0.1:4218',
    reuseExistingServer: false,
  },
});
