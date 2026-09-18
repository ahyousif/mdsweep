import { expect, test } from '@playwright/test';

type Passenger = {
  id: string;
  brokerMemberId: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  phoneNumber: string | null;
  alternatePhoneNumber: string | null;
  passengerType: string | null;
  specialNeeds: string | null;
  notes: string | null;
  isActive: boolean;
};

for (const language of ['en', 'ar'] as const) {
  test(`Passenger inspector edits and disables with confirmation in ${language}`, async ({
    page,
  }) => {
    await page.addInitScript((value) => localStorage.setItem('mdsweep.language', value), language);
    const tenant = { id: 'mdsw-eep2-3456', name: 'Synthetic Tenant', roles: ['Dispatcher'] };
    await page.route('**/api/auth/**', (route) =>
      route.fulfill({
        json: route.request().url().endsWith('/session')
          ? {
              userId: 'admin',
              displayName: 'Synthetic Dispatcher',
              activeTenant: tenant,
              availableTenants: [tenant],
            }
          : { token: 'synthetic-token' },
      }),
    );
    let passenger: Passenger = {
      id: 'synthetic-passenger',
      brokerMemberId: 'MEMBER-42',
      firstName: 'Taylor',
      lastName: 'Example',
      dateOfBirth: '1995-06-08',
      phoneNumber: '6028992207',
      alternatePhoneNumber: null,
      passengerType: 'Ambulatory',
      specialNeeds: null,
      notes: null,
      isActive: true,
    };
    await page.route('**/api/passengers**', (route) => {
      const request = route.request();
      const path = new URL(request.url()).pathname;
      if (request.method() === 'GET' && path === '/api/passengers') {
        return route.fulfill({
          json: { items: [passenger], totalCount: 1, page: 1, pageSize: 25, totalPages: 1 },
        });
      }
      if (request.method() === 'GET') return route.fulfill({ json: passenger });
      if (request.method() === 'PUT') {
        expect(request.postDataJSON()).not.toHaveProperty('isActive');
        passenger = { ...passenger, ...request.postDataJSON() };
        return route.fulfill({ status: 204 });
      }
      if (request.method() === 'POST' && path.endsWith('/disable')) {
        passenger = { ...passenger, isActive: false };
        return route.fulfill({ status: 204 });
      }
      if (request.method() === 'POST' && path.endsWith('/enable')) {
        passenger = { ...passenger, isActive: true };
        return route.fulfill({ status: 204 });
      }
      return route.fulfill({ status: 404 });
    });

    await page.goto('/passengers');
    const row = page.getByRole('row', { name: /Taylor Example/ });
    await row.click();
    await expect(row).toHaveAttribute('aria-selected', 'true');
    const inspector = page.locator('aside');
    const close = inspector.getByRole('button', {
      name: language === 'en' ? 'Close passenger details' : 'إغلاق تفاصيل الراكب',
    });
    await expect(close).toBeVisible();
    await expect(close).not.toHaveClass(/text-destructive/);
    await expect(inspector.getByText('MEMBER-42')).toHaveCount(1);
    const memberId = await inspector.locator('header').getByText('MEMBER-42').boundingBox();
    const status = await inspector
      .locator('header')
      .getByText(language === 'en' ? 'Active' : 'نشط', { exact: true })
      .boundingBox();
    expect(
      memberId &&
        status &&
        Math.abs(memberId.y + memberId.height / 2 - status.y - status.height / 2),
    ).toBeLessThan(8);
    const edit = language === 'en' ? 'Edit' : 'تعديل';
    const back = language === 'en' ? 'Back to details' : 'العودة إلى التفاصيل';
    const save = language === 'en' ? 'Save changes' : 'حفظ التغييرات';
    const notes = language === 'en' ? 'Notes' : 'ملاحظات';
    const disable = language === 'en' ? 'Disable passenger' : 'تعطيل الراكب';
    const disableShort = language === 'en' ? 'Disable' : 'تعطيل';
    const enableShort = language === 'en' ? 'Enable' : 'تفعيل';
    const detailsHeading = inspector.getByRole('heading', {
      name: language === 'en' ? 'Details' : 'التفاصيل',
    });
    await expect(
      detailsHeading.locator('..').getByRole('button', { name: edit, exact: true }),
    ).toBeVisible();
    await expect(
      detailsHeading.locator('..').getByRole('button', { name: disableShort, exact: true }),
    ).toBeVisible();
    await expect(
      inspector.getByRole('button', {
        name: language === 'en' ? 'Passenger actions' : 'إجراءات الراكب',
      }),
    ).toHaveCount(0);
    await inspector.getByRole('button', { name: edit, exact: true }).click();
    await expect(
      inspector.getByRole('heading', {
        name: language === 'en' ? 'Edit passenger' : 'تعديل الراكب',
      }),
    ).toBeVisible();
    await inspector.getByLabel(notes).fill('Synthetic morning pickup');
    await inspector.getByRole('button', { name: back }).click();
    await expect(inspector.getByText('Synthetic morning pickup')).toHaveCount(0);
    await expect(row).toHaveAttribute('aria-selected', 'true');
    await inspector.getByRole('button', { name: edit, exact: true }).click();
    await inspector.getByLabel(notes).fill('Synthetic morning pickup');
    await inspector.getByRole('button', { name: save }).click();
    await expect(inspector.getByText('Synthetic morning pickup')).toBeVisible();
    await inspector.getByRole('button', { name: disableShort, exact: true }).click();
    const confirmation = page.getByRole('dialog');
    await expect(confirmation).toBeVisible();
    await expect(confirmation).toContainText(
      language === 'en' ? 'historical data will be retained' : 'سجله السابق',
    );
    await confirmation.getByRole('button', { name: disable }).click();
    await expect(confirmation).toHaveCount(0);
    await expect(
      inspector.getByText(language === 'en' ? 'Disabled' : 'معطّل', { exact: true }),
    ).toBeVisible();
    expect(passenger.isActive).toBe(false);
    await inspector.getByRole('button', { name: edit, exact: true }).click();
    await inspector
      .getByRole('textbox', { name: notes, exact: true })
      .fill('Updated while disabled');
    await inspector.getByRole('button', { name: save }).click();
    await expect(inspector.getByText('Updated while disabled')).toBeVisible();
    await expect(
      inspector.getByText(language === 'en' ? 'Disabled' : 'معطّل', { exact: true }),
    ).toBeVisible();
    await inspector.getByRole('button', { name: enableShort, exact: true }).click();
    await expect(
      inspector.getByText(language === 'en' ? 'Active' : 'نشط', { exact: true }),
    ).toBeVisible();
    await page.setViewportSize({ width: 390, height: 700 });
    await expect(close).toHaveCount(0);
    const backToList = inspector.getByRole('button', {
      name: language === 'en' ? 'Back to passengers' : 'العودة إلى الركاب',
    });
    await expect(backToList).toBeVisible();
    await inspector.getByRole('button', { name: edit, exact: true }).click();
    await expect(
      inspector.getByRole('heading', {
        name: language === 'en' ? 'Edit passenger' : 'تعديل الراكب',
      }),
    ).toBeVisible();
    await inspector.getByRole('button', { name: back }).click();
    await backToList.click();
    await expect(inspector).toHaveCount(0);
  });
}
