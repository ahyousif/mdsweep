import { expect, test, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

async function mockApplication(page: Page) {
  await page.route('**/api/auth/**', (route) =>
    route.fulfill({
      json: {
        userId: 'synthetic-user',
        displayName: 'Synthetic Administrator',
        email: 'synthetic@example.test',
        activeTenant: { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Administrator'] },
        availableTenants: [
          { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Administrator'] },
        ],
      },
    }),
  );
  await page.route('**/api/users', (route) =>
    route.request().method() === 'GET'
      ? route.fulfill({ json: [] })
      : route.fulfill({ status: 204 }),
  );
  await page.route('**/api/trips?*', (route) =>
    route.fulfill({
      json: {
        items: [
          {
            id: 'synthetic-trip',
            brokerTripNumber: 'SYN-123-A',
            passengerFirstName: 'راكب',
            passengerLastName: 'تجريبي',
            memberId: 'SYN-123',
            serviceDate: '2026-09-12',
            direction: 'To',
            brokerStatus: 'VALID',
            isWillCall: false,
            passengerType: null,
            specialNeeds: 'Synthetic note',
            tripCost: null,
            tripMileage: null,
            appointmentTime: '10:00:00',
            returnPickupTime: null,
            scheduledPickupTime: '09:30:00',
            calculatedPickupTime: '09:30:00',
            manualPickupTime: null,
            estimatedTravelMinutes: 20,
            estimatedDistanceMeters: 1609,
            pickup: { address: '100 Synthetic Way', city: 'Phoenix', state: 'AZ', zip: '85001' },
            dropoff: { address: '200 Example Way', city: 'Phoenix', state: 'AZ', zip: '85001' },
          },
        ],
        totalCount: 1,
        page: 1,
        pageSize: 100,
        totalPages: 1,
      },
    }),
  );
}

async function switchLanguage(page: Page, language: 'en' | 'ar') {
  await page.getByRole('button', { name: /Open user menu|فتح قائمة المستخدم/ }).click();
  await page
    .getByRole('menuitemradio', { name: language === 'ar' ? 'العربية' : 'English', exact: true })
    .click();
  await expect(page.locator('html')).toHaveAttribute('lang', language);
  await page.keyboard.press('Escape');
}

test('switches the complete Trips screen and preserves selection, filters, and dates', async ({
  page,
}) => {
  await mockApplication(page);
  await page.goto('/trips');
  await expect(page.getByRole('heading', { name: 'Trips', exact: true })).toBeVisible();
  const search = page.getByRole('searchbox');
  await search.fill('SYN-123');
  await page.getByRole('button').filter({ hasText: 'SYN-123-A' }).click();
  const originalUrl = page.url();
  await switchLanguage(page, 'ar');
  await expect(page).toHaveURL(originalUrl);
  await expect(search).toHaveValue('SYN-123');
  await expect(page.getByRole('heading', { name: 'الرحلات', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'إغلاق تفاصيل الرحلة' })).toBeVisible();
  await expect(page.getByText('تفاصيل الرحلة', { exact: true })).toBeVisible();
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
  await expect(page.locator('hlm-sidebar')).toHaveAttribute('data-side', 'right');
  await expect(page.locator('bdi').filter({ hasText: 'SYN-123-A' }).first()).toBeVisible();
  await page.getByRole('button', { name: 'إغلاق تفاصيل الرحلة' }).click();
  await page.locator('#service-date').click();
  await expect(page.getByRole('button', { name: 'الشهر التالي' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'الشهر السابق' })).toBeVisible();
  await page.keyboard.press('Escape');
  await page.reload();
  await expect(page.getByRole('heading', { name: 'الرحلات', exact: true })).toBeVisible();
  await switchLanguage(page, 'en');
  await expect(page.locator('html')).toHaveAttribute('dir', 'ltr');
  await expect(page.getByRole('heading', { name: 'Trips', exact: true })).toBeVisible();
});

test('retains unfinished forms and translates an already-visible server error', async ({
  page,
}) => {
  await page.setViewportSize({ width: 1440, height: 1000 });
  await mockApplication(page);
  await page.route('**/api/users', (route) =>
    route.fulfill({
      json: [
        {
          id: 'synthetic-user',
          type: 'User',
          firstName: 'Synthetic',
          lastName: 'Administrator',
          displayName: 'Synthetic Administrator',
          email: 'synthetic@example.test',
          roles: ['Administrator'],
          status: 'Active',
          expiresAt: null,
        },
      ],
    }),
  );
  await page.route('**/api/users/synthetic-user', (route) =>
    route.fulfill({
      status: 400,
      json: {
        errors: { user: ['Cannot remove own access'] },
        localizedErrors: [{ field: 'user', code: 'protectOwnAccess' }],
      },
    }),
  );
  await page.goto('/users');
  await page.getByRole('row').filter({ hasText: 'synthetic@example.test' }).click();
  await page.getByRole('button', { name: 'Edit', exact: true }).click();
  await page.getByLabel('Display name', { exact: true }).fill('Unfinished synthetic name');
  await page.getByRole('button', { name: 'Save changes' }).click();
  await expect(page.getByRole('alert')).toContainText('You cannot deactivate yourself');
  await switchLanguage(page, 'ar');
  await expect(page.getByLabel('اسم العرض', { exact: true })).toHaveValue(
    'Unfinished synthetic name',
  );
  await expect(page.getByRole('alert')).toContainText('لا يمكنك تعطيل حسابك');
  await switchLanguage(page, 'en');
  await expect(page.getByLabel('Display name', { exact: true })).toHaveValue(
    'Unfinished synthetic name',
  );
  await expect(page.getByRole('alert')).toContainText('You cannot deactivate yourself');
});

test('Arabic import feedback preserves the filename and required broker column names', async ({
  page,
}) => {
  await mockApplication(page);
  await page.route('**/api/trips/import', (route) =>
    route.fulfill({
      json: {
        readyCount: 0,
        needsAttentionCount: 1,
        problems: [
          {
            rowNumber: null,
            tripNumber: null,
            field: null,
            message: 'Missing required columns: Medicaid Number',
            code: 'manifest.missingColumns',
            parameters: { columns: 'Medicaid Number' },
          },
        ],
      },
    }),
  );
  await page.goto('/trips');
  await switchLanguage(page, 'ar');
  await page.getByRole('button', { name: 'استيراد الرحلات', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await dialog.locator('input[type=file]').setInputFiles({
    name: 'synthetic-manifest.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from('Trip Number\nSYN-123\n'),
  });
  await dialog.getByRole('button', { name: 'استيراد الرحلات', exact: true }).click();
  await expect(dialog.getByRole('heading', { name: 'اكتمل الاستيراد' })).toBeVisible();
  await expect(dialog.getByText('synthetic-manifest.csv')).toBeVisible();
  await expect(dialog.getByText(/يفتقد ملف الرحلات الأعمدة المطلوبة التالية/)).toContainText(
    'Medicaid Number',
  );
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

test('mobile navigation has an accessible English title and restores focus after dismissal', async ({
  page,
}) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await mockApplication(page);
  await page.goto('/trips');
  const trigger = page.getByRole('button', { name: 'Toggle navigation' });
  await trigger.click();
  await expect(page.getByRole('dialog')).toHaveAccessibleName('Main navigation');
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(trigger).toBeFocused();
});

for (const theme of ['light', 'dark']) {
  for (const mobile of [false, true]) {
    test(`Arabic is readable and accessible in ${theme} theme on ${mobile ? 'mobile' : 'desktop'}`, async ({
      page,
    }, testInfo) => {
      await page.setViewportSize(
        mobile ? { width: 390, height: 844 } : { width: 1440, height: 1000 },
      );
      await page.addInitScript((theme) => {
        localStorage.setItem('mdsweep.language', 'ar');
        localStorage.setItem('mdsweep.theme', theme);
      }, theme);
      await mockApplication(page);
      await page.goto('/trips');
      await expect(page.getByRole('heading', { name: 'الرحلات', exact: true })).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
        true,
      );
      if (mobile) {
        await page.getByRole('button', { name: 'إظهار قائمة التنقل أو إخفاؤها' }).click();
        await expect(page.getByRole('dialog')).toHaveAccessibleName('التنقل الرئيسي');
        await expect(page.getByRole('link', { name: 'المستخدمون' })).toBeVisible();
        expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
        await page.keyboard.press('Escape');
        await expect(page.getByRole('dialog')).toHaveCount(0);
      }
      const result = await new AxeBuilder({ page }).analyze();
      expect(result.violations).toEqual([]);
      await page.screenshot({
        path: testInfo.outputPath(`arabic-${theme}-${mobile ? 'mobile' : 'desktop'}.png`),
        fullPage: true,
      });
    });
  }
}
