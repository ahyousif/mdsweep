import { expect, test, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

type Vehicle = { id: string; displayLabel: string; vin: string; isActive: boolean };
const vin = '1M8GDM9AXKP042788';
const spareVin = '1M8GDM9AXKP042789';

// Synthetic browser fixtures exercise interactions; PostgreSQL HTTP tests cover the real API.
async function setup(page: Page, role = 'Dispatcher') {
  const tenant = { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: [role] };
  await page.route('**/api/auth/**', (route) =>
    route.fulfill({
      json: route.request().url().endsWith('/session')
        ? {
            userId: 'synthetic-user',
            displayName: 'Synthetic Dispatcher',
            email: 'dispatcher@example.test',
            activeTenant: tenant,
            availableTenants: [tenant],
          }
        : { token: 'synthetic-token' },
    }),
  );
  let vehicles: Vehicle[] = [
    { id: 'spare', displayLabel: 'Spare van', vin: spareVin, isActive: false },
  ];
  await page.route('**/api/vehicles**', async (route) => {
    const request = route.request();
    if (request.method() === 'GET') return route.fulfill({ json: vehicles });
    const body = request.postDataJSON();
    const path = new URL(request.url()).pathname.split('/');
    const id = path[3];
    if (!path.includes('active') && vehicles.some((v) => v.vin === body.vin && v.id !== id))
      return route.fulfill({
        status: 400,
        json: {
          errors: { vin: ['A vehicle with this VIN already exists.'] },
          issues: [{ field: 'vin', code: 'vehicleVinExists' }],
        },
      });
    if (request.method() === 'POST') {
      const vehicle = { ...body, id: 'new-vehicle', isActive: true };
      vehicles.push(vehicle);
      return route.fulfill({ status: 201, json: vehicle });
    }
    vehicles = vehicles.map((vehicle) => (vehicle.id === id ? { ...vehicle, ...body } : vehicle));
    return route.fulfill({ status: 204 });
  });
}

for (const language of ['en', 'ar'] as const) {
  const en = language === 'en';
  const labels = {
    add: en ? 'Add vehicle' : 'إضافة مركبة',
    edit: en ? 'Edit' : 'تعديل',
    save: en ? 'Save changes' : 'حفظ التغييرات',
    search: en ? 'Search by label or VIN' : 'البحث بالاسم أو رقم تعريف المركبة',
    deactivate: en ? 'Deactivate vehicle' : 'تعطيل المركبة',
    reactivate: en ? 'Reactivate vehicle' : 'إعادة تفعيل المركبة',
    close: en ? 'Close vehicle details' : 'إغلاق تفاصيل المركبة',
  };
  test(`vehicle details keep keyboard focus on mobile in ${language}`, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.addInitScript((lang) => localStorage.setItem('mdsweep.language', lang), language);
    await setup(page);
    await page.route('**/api/vehicles', (route) =>
      route.fulfill({
        json: [
          { id: 'one', displayLabel: 'Van 1', vin, isActive: true },
          { id: 'two', displayLabel: 'Van 2', vin: spareVin, isActive: true },
        ],
      }),
    );
    await page.goto('/vehicles');
    const trigger = page.getByRole('button', { name: 'Van 1', exact: true });
    const close = page.getByRole('button', { name: labels.close });
    const edit = page.getByRole('button', { name: labels.edit, exact: true });
    await trigger.focus();
    await page.keyboard.press('Enter');
    await expect(close).toBeFocused();
    await expect(page.locator('app-vehicles-page section')).toHaveJSProperty('inert', true);
    await page.keyboard.press('Tab');
    await expect(edit).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(page.getByRole('button', { name: labels.deactivate })).toBeFocused();
    await page.keyboard.press('Tab');
    await expect(close).toBeFocused();
    await page.keyboard.press('Shift+Tab');
    await expect(page.getByRole('button', { name: labels.deactivate })).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(trigger).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(close).toBeFocused();
    await page.keyboard.press('Tab');
    await page.keyboard.press('Enter');
    await expect(page.getByRole('dialog').locator('#vehicle-label')).toBeFocused();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('dialog')).not.toBeVisible();
    await expect(edit).toBeFocused();
    await page.setViewportSize({ width: 1279, height: 1000 });
    await expect(page.locator('app-vehicles-page section')).toHaveJSProperty('inert', true);
    await expect(page.locator('#vehicle-detail')).toHaveCSS('position', 'absolute');
    await page.setViewportSize({ width: 1280, height: 1000 });
    await expect(page.locator('app-vehicles-page section')).toHaveJSProperty('inert', false);
    await expect(page.locator('#vehicle-detail')).toHaveCSS('position', 'static');
    await page.getByRole('searchbox').focus();
    await expect(page.getByRole('searchbox')).toBeFocused();
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(close).toBeFocused();
    await page.keyboard.press('Enter');
    await expect(trigger).toBeFocused();
    await expect(page.locator('#vehicle-detail')).toHaveCount(0);
  });
  test(`vehicle management, duplicate recovery, and accessibility in ${language}`, async ({
    page,
  }, testInfo) => {
    await page.setViewportSize({ width: 1536, height: 1000 });
    await page.addInitScript((lang) => {
      localStorage.setItem('mdsweep.language', lang);
      localStorage.setItem('mdsweep.theme', 'light');
    }, language);
    await setup(page);
    await page.goto('/vehicles');
    await expect(
      page.getByRole('heading', { name: en ? 'Vehicles' : 'المركبات', exact: true }),
    ).toBeVisible();
    await page.getByRole('button', { name: labels.add, exact: true }).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByRole('button', { name: labels.add, exact: true }).click();
    await expect(dialog.locator('#vehicle-label-error')).not.toBeEmpty();
    await expect(dialog.locator('#vehicle-vin-error')).not.toBeEmpty();
    await dialog.locator('#vehicle-label').fill('Van 1');
    await dialog.locator('#vehicle-vin').fill(spareVin);
    await dialog.getByRole('button', { name: labels.add, exact: true }).click();
    await expect(dialog.getByRole('alert')).toContainText(en ? 'already exists' : 'توجد مركبة');
    await expect(dialog.locator('#vehicle-label')).toHaveValue('Van 1');
    await dialog.locator('#vehicle-year').fill('1899');
    await dialog.getByRole('button', { name: labels.add, exact: true }).click();
    await expect(dialog.locator('#vehicle-year-error')).not.toBeEmpty();
    await dialog.locator('#vehicle-year').fill('2022');
    await dialog.locator('#vehicle-make').fill('Toyota');
    await dialog.locator('#vehicle-model').fill('Sienna');
    await dialog.locator('#vehicle-vin').fill(vin.toLowerCase());
    await dialog.getByRole('button', { name: labels.add, exact: true }).click();
    await expect(dialog).not.toBeVisible();
    await page.getByRole('searchbox', { name: labels.search }).fill('Van 1');
    await page.getByRole('button', { name: 'Van 1', exact: true }).click();
    await expect(page.locator('#vehicle-detail')).toContainText(vin.toLowerCase());
    await expect(page.locator('#vehicle-detail')).toContainText('2022');
    await expect(page.locator('#vehicle-detail')).toContainText('Toyota');
    await expect(page.locator('#vehicle-detail')).toContainText('Sienna');
    for (const term of ['2022', 'Toyota', 'Sienna']) {
      await page.getByRole('searchbox', { name: labels.search }).fill(term);
      await expect(page.getByRole('button', { name: 'Van 1', exact: true })).toBeVisible();
    }
    await page.getByRole('searchbox', { name: labels.search }).fill('Van 1');
    await page.screenshot({
      path: testInfo.outputPath(`vehicles-${language}.png`),
      fullPage: true,
      animations: 'disabled',
    });
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);

    await page.getByRole('button', { name: labels.edit, exact: true }).click();
    await expect(dialog.locator('#vehicle-year')).toHaveValue('2022');
    await expect(dialog.locator('#vehicle-make')).toHaveValue('Toyota');
    await expect(dialog.locator('#vehicle-model')).toHaveValue('Sienna');
    await dialog.locator('#vehicle-year').fill('');
    await dialog.locator('#vehicle-make').fill('');
    await dialog.locator('#vehicle-model').fill('');
    await dialog.locator('#vehicle-label').fill('Van 1 updated');
    await dialog.getByRole('button', { name: labels.save, exact: true }).click();
    await expect(dialog).not.toBeVisible();
    await expect(page.getByRole('searchbox')).toHaveValue('Van 1');
    await expect(page.locator('#vehicle-detail')).toContainText('Van 1 updated');
    await expect(page.locator('#vehicle-detail')).not.toContainText('Toyota');
    await page.getByRole('button', { name: labels.deactivate, exact: true }).click();
    await expect(page.getByRole('button', { name: labels.reactivate, exact: true })).toBeVisible();
    await page.getByRole('button', { name: labels.reactivate, exact: true }).click();
    await expect(page.getByRole('button', { name: labels.deactivate, exact: true })).toBeVisible();
    await page.getByRole('button', { name: labels.close, exact: true }).click();
    await page.getByRole('searchbox').fill('');
    await page
      .getByRole('navigation', {
        name: en ? 'Filter vehicles by status' : 'تصفية المركبات حسب الحالة',
      })
      .getByRole('button', { name: en ? 'Inactive 1' : 'غير نشطة 1', exact: true })
      .click();
    await expect(page.getByRole('button', { name: 'Spare van', exact: true })).toBeVisible();
    await expect(
      page.getByRole('button', { name: 'Van 1 updated', exact: true }),
    ).not.toBeVisible();

    // Reflow/RTL and forms remain usable on narrow screens.
    await page.setViewportSize({ width: 390, height: 844 });
    await page.getByRole('button', { name: 'Spare van', exact: true }).click();
    await expect(page.getByRole('button', { name: labels.reactivate, exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(
      true,
    );
    await page.getByRole('button', { name: labels.edit, exact: true }).click();
    await expect(dialog.locator('#vehicle-vin')).toHaveAttribute('dir', 'ltr');
    await page.screenshot({
      path: testInfo.outputPath(`vehicles-mobile-${language}.png`),
      fullPage: true,
      animations: 'disabled',
    });
    expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
    await page.keyboard.press('Escape');
    await expect(dialog).not.toBeVisible();
    await page.getByRole('button', { name: labels.reactivate, exact: true }).click();
    await expect(page.getByRole('button', { name: labels.deactivate, exact: true })).toBeEnabled();
    await page.getByRole('button', { name: labels.close, exact: true }).click();
    // Reactivating removes the trigger row from the Inactive filter.
    await expect(page.getByRole('searchbox')).toBeFocused();
  });
}

test('live language switching preserves search, selection, and feedback', async ({ page }) => {
  await page.setViewportSize({ width: 1536, height: 1000 });
  await setup(page);
  await page.goto('/vehicles');
  await page.getByRole('searchbox').fill('Spare');
  await page.getByRole('button', { name: 'Spare van', exact: true }).click();
  await page.getByRole('button', { name: 'Reactivate vehicle', exact: true }).click();
  await expect(page.getByRole('status')).toContainText('Vehicle reactivated.');
  await page.getByRole('button', { name: 'Open user menu' }).click();
  await page.getByRole('menuitemradio', { name: 'العربية', exact: true }).click();
  await expect(page.locator('html')).toHaveAttribute('dir', 'rtl');
  await expect(page.getByRole('searchbox')).toHaveValue('Spare');
  await expect(page.locator('#vehicle-detail')).toContainText('Spare van');
  await expect(page.getByRole('status')).toContainText('تمت إعادة تفعيل المركبة.');
});

test('loading failure is actionable and an empty list can recover', async ({ page }) => {
  await setup(page, 'Administrator');
  let failing = true;
  await page.route('**/api/vehicles', (route) =>
    failing ? route.fulfill({ status: 500, json: {} }) : route.fulfill({ json: [] }),
  );
  await page.goto('/vehicles');
  await expect(page.getByRole('button', { name: 'Retry', exact: true })).toBeVisible({
    timeout: 15000,
  });
  failing = false;
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  await expect(page.getByText('No vehicles yet.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Add vehicle', exact: true })).toBeEnabled();
});

test('a Driver cannot open vehicle management', async ({ page }) => {
  await setup(page, 'Driver');
  let requests = 0;
  await page.route('**/api/vehicles**', (route) => {
    requests++;
    return route.fulfill({ json: [] });
  });
  await page.goto('/vehicles');
  await expect(page.getByRole('heading', { name: 'Vehicles', exact: true })).not.toBeVisible();
  await expect(page.getByRole('link', { name: 'Vehicles', exact: true })).not.toBeVisible();
  await expect(page.getByText('My Trips is not available in this build yet.')).toBeVisible();
  expect(requests).toBe(0);
});
