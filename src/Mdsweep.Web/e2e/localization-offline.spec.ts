import { expect, test } from '@playwright/test';

test('installed PWA switches languages offline and restores the preference after reload', async ({
  page,
  context,
}) => {
  await page.goto('/users');
  await expect(page.getByRole('heading', { name: 'Users', exact: true })).toBeVisible();
  await page.evaluate(async () => {
    await navigator.serviceWorker.ready;
  });
  await page.reload();
  await expect.poll(() => page.evaluate(() => !!navigator.serviceWorker.controller)).toBe(true);
  await expect
    .poll(() =>
      page.evaluate(async () => {
        const english = await caches.match('/i18n/en.json');
        const arabic = await caches.match('/i18n/ar.json');
        return !!english && !!arabic;
      }),
    )
    .toBe(true);
  await expect(page.getByRole('heading', { name: 'Users', exact: true })).toBeVisible();
  await context.setOffline(true);
  await page.getByRole('button', { name: 'Open user menu' }).click();
  await page.getByRole('menuitemradio', { name: 'العربية', exact: true }).click();
  await page.keyboard.press('Escape');
  await expect(page.getByRole('heading', { name: 'المستخدمون', exact: true })).toBeVisible();
  await page.reload();
  // Session data is not persisted: the offline recovery screen still supports both languages.
  await expect(page.getByRole('heading', { name: 'تعذر إنشاء جلستك' })).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('lang', 'ar');
  await page.getByRole('combobox', { name: 'اللغة' }).selectOption('en');
  await expect(
    page.getByRole('heading', { name: 'Unable to establish your session' }),
  ).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');
  await context.setOffline(false);
});
