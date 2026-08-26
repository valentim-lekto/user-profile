import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './specs',
  outputDir: '/artifacts/test-results',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 120_000,
  expect: {
    timeout: 30_000,
  },
  reporter: [
    ['line'],
    ['html', { open: 'never', outputFolder: '/artifacts/playwright-report' }],
    ['junit', { outputFile: '/artifacts/junit.xml' }],
  ],
  use: {
    baseURL: process.env['E2E_BASE_URL'] ?? 'http://web-e2e:8080',
    screenshot: 'only-on-failure',
    trace: {
      mode: 'retain-on-failure',
      attachments: false,
      screenshots: false,
      snapshots: false,
      sources: false,
    },
    video: 'off',
  },
});
