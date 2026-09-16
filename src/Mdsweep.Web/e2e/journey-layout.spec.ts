import { expect, test, type Page } from '@playwright/test';

async function mockTrips(page: Page) {
  await page.route('**/api/auth/**', (route) =>
    route.fulfill({
      json: {
        userId: 'synthetic-user',
        displayName: 'Synthetic Dispatcher',
        email: 'dispatcher@example.test',
        activeTenant: {
          id: 'synthetic-tenant',
          name: 'Synthetic Tenant',
          roles: ['Administrator'],
        },
        availableTenants: [
          { id: 'synthetic-tenant', name: 'Synthetic Tenant', roles: ['Administrator'] },
        ],
      },
    }),
  );
  await page.route('**/api/trips?*', (route) =>
    route.fulfill({
      json: {
        items: [
          trip('outbound', 'OUT-100', '09:00:00', '100 Synthetic St', '200 Example Ave'),
          {
            ...trip('return', 'RETURN-200', '13:00:00', '200 Example Ave', '100 Synthetic St'),
            lifecycleStatus: 'completed',
            assignment: { driverName: 'Driver 3', vehicleName: 'Van 2' },
          },
        ],
        totalCount: 2,
        page: 1,
        pageSize: 100,
        totalPages: 1,
      },
    }),
  );
}

function trip(id: string, number: string, time: string, pickup: string, dropoff: string) {
  return {
    id,
    journeyId: 'synthetic-journey',
    brokerTripNumber: number,
    passengerFirstName: 'Synthetic',
    passengerLastName: 'Passenger',
    memberId: null,
    serviceDate: '2026-09-15',
    direction: id === 'return' ? 'From' : 'To',
    brokerStatus: 'VALID',
    isWillCall: false,
    passengerType: null,
    specialNeeds: null,
    tripCost: null,
    tripMileage: null,
    appointmentTime: id === 'return' ? null : '10:00:00',
    returnPickupTime: id === 'return' ? time : null,
    scheduledPickupTime: time,
    calculatedPickupTime: null,
    manualPickupTime: null,
    estimatedTravelMinutes: null,
    estimatedDistanceMeters: null,
    pickup: { address: pickup, city: 'Phoenix', state: 'AZ', zip: '85001' },
    dropoff: { address: dropoff, city: 'Phoenix', state: 'AZ', zip: '85001' },
  };
}

async function expectAlignedColumns(page: Page, direction: 'ltr' | 'rtl') {
  const edges = await page.evaluate((direction) => {
    const header = Array.from(
      document.querySelectorAll<HTMLElement>('[data-journey-header] > span'),
    );
    const row = Array.from(document.querySelectorAll<HTMLElement>('[data-journey-row] > *'));
    return header.slice(0, 5).map((cell, index) => {
      const headerBox = cell.getBoundingClientRect();
      const rowBox = row[index].getBoundingClientRect();
      return direction === 'rtl'
        ? Math.abs(headerBox.right - rowBox.right)
        : Math.abs(headerBox.left - rowBox.left);
    });
  }, direction);
  expect(edges).toHaveLength(5);
  expect(Math.max(...edges)).toBeLessThanOrEqual(2);
}

async function expectDrawerTabsFit(page: Page) {
  const layout = await page.locator('app-trip-detail [data-slot="tabs-list"]').evaluate((list) => {
    const tabs = Array.from(list.querySelectorAll<HTMLElement>('[data-slot="tabs-trigger"]'));
    const widths = tabs.map((tab) => tab.getBoundingClientRect().width);
    return {
      display: getComputedStyle(list).display,
      overflow: list.scrollWidth - list.clientWidth,
      count: tabs.length,
      widths,
      clippedLabels: tabs.some((tab) => tab.scrollWidth > tab.clientWidth + 1),
    };
  });

  expect(layout.display).toBe('grid');
  expect(layout.count).toBe(5);
  expect(layout.overflow).toBeLessThanOrEqual(1);
  expect(Math.max(...layout.widths) - Math.min(...layout.widths)).toBeLessThanOrEqual(1);
  expect(layout.clippedLabels).toBe(false);
}

for (const language of ['en', 'ar'] as const) {
  test(`Journey header follows the list pane and drawer cards align in ${language}`, async ({
    page,
  }) => {
    await page.setViewportSize({ width: 1920, height: 900 });
    await page.addInitScript(
      (language) => localStorage.setItem('mdsweep.language', language),
      language,
    );
    await mockTrips(page);
    await page.goto('/trips');

    const header = page.locator('[data-journey-header]');
    const row = page.locator('[data-journey-row]');
    await expect(row).toHaveCount(1);
    await expect(header).toBeVisible();
    await expectAlignedColumns(page, language === 'ar' ? 'rtl' : 'ltr');
    await expect(
      page.getByRole('button', {
        name: new RegExp(language === 'ar' ? 'قيد التنفيذ' : 'In progress'),
      }),
    ).toBeDisabled();
    await expect(
      page.getByRole('button', { name: new RegExp(language === 'ar' ? 'مكتملة' : 'Completed') }),
    ).toBeDisabled();

    await page.getByRole('searchbox').fill('RETURN-200');
    await expect(row).toHaveCount(1);
    await expect(row).toContainText(language === 'ar' ? 'رحلتان' : '2 trips');

    const menu = page.locator('app-trip-card button[data-slot="dropdown-menu-trigger"]');
    await expect(page.locator('app-trip-card ng-icon[name="lucideChevronRight"]')).toHaveCount(0);
    await expect(menu.locator('ng-icon[name="lucideEllipsisVertical"]')).toBeVisible();
    await menu.click();
    await expect(row).toHaveAttribute('aria-pressed', 'false');
    await page.keyboard.press('Escape');
    await row.focus();
    await page.keyboard.press('Enter');
    await expect(row).toHaveAttribute('aria-pressed', 'true');
    await expectDrawerTabsFit(page);
    const closeButton = page.getByRole('button', {
      name: language === 'ar' ? 'إغلاق تفاصيل الرحلة' : 'Close trip details',
    });
    await expect(closeButton).toHaveClass(/text-destructive/);
    const closeSize = await closeButton.evaluate((button) => {
      const bounds = button.getBoundingClientRect();
      const icon = button.querySelector('ng-icon')!;
      return {
        width: bounds.width,
        height: bounds.height,
        iconSize: getComputedStyle(icon).fontSize,
      };
    });
    expect(closeSize.width).toBeGreaterThanOrEqual(36);
    expect(closeSize.height).toBeGreaterThanOrEqual(36);
    expect(parseFloat(closeSize.iconSize)).toBeGreaterThanOrEqual(20);
    await expect(header).toBeVisible();
    await expectAlignedColumns(page, language === 'ar' ? 'rtl' : 'ltr');

    const journeyCard = page.locator('[data-journey-card]');
    const route = journeyCard.locator('[data-journey-route]');
    const routeText = await route.textContent();
    expect(routeText?.match(/100 Synthetic St/g)).toHaveLength(1);
    expect(routeText?.match(/200 Example Ave/g)).toHaveLength(1);

    const journeyLayout = await journeyCard.evaluate((card) => {
      const heading = card.querySelector('h3')!.getBoundingClientRect();
      const action = card.querySelector('a')!.getBoundingClientRect();
      const stops = card.querySelector('[data-journey-route]')!.getBoundingClientRect();
      const facts = card.querySelector('dl')!.getBoundingClientRect();
      return {
        actionHeadingTopDelta: Math.abs(action.top - heading.top),
        stops: { left: stops.left, right: stops.right, top: stops.top },
        facts: { left: facts.left, right: facts.right, top: facts.top },
      };
    });
    expect(journeyLayout.actionHeadingTopDelta).toBeLessThanOrEqual(8);
    expect(Math.abs(journeyLayout.stops.top - journeyLayout.facts.top)).toBeLessThanOrEqual(1);
    if (language === 'ar') {
      expect(journeyLayout.facts.right).toBeLessThan(journeyLayout.stops.left);
    } else {
      expect(journeyLayout.stops.right).toBeLessThan(journeyLayout.facts.left);
    }

    const cards = await page.locator('app-trip-detail [hlmCard]').evaluateAll((elements) =>
      elements
        .map((element) => {
          const bounds = element.getBoundingClientRect();
          return { left: bounds.left, right: bounds.right, width: bounds.width };
        })
        .filter((bounds) => bounds.width > 0),
    );
    expect(cards).toHaveLength(4);
    for (const card of cards.slice(1)) {
      expect(Math.abs(card.left - cards[0].left)).toBeLessThanOrEqual(1);
      expect(Math.abs(card.right - cards[0].right)).toBeLessThanOrEqual(1);
    }

    const actionAlignment = await page
      .locator('app-trip-detail [hlmCard]')
      .evaluateAll((elements) =>
        elements
          .filter((card) => card.getBoundingClientRect().width > 0)
          .map((card) => {
            const heading = card
              .querySelector('[data-slot="card-header"] h3')!
              .getBoundingClientRect();
            const action = card
              .querySelector('[data-slot="card-header"] [data-slot="card-action"]')!
              .getBoundingClientRect();
            return Math.abs(heading.top - action.top);
          }),
      );
    expect(actionAlignment).toHaveLength(4);
    expect(Math.max(...actionAlignment)).toBeLessThanOrEqual(8);

    const tripRows = page.locator('app-trip-detail [data-trip-row]');
    await expect(tripRows).toHaveCount(2);
    await expect(
      page.getByRole('heading', {
        name: language === 'ar' ? 'رحلات المجموعة' : 'Journey trips',
      }),
    ).toBeVisible();
    await expect(
      page.getByRole('button', { name: language === 'ar' ? 'إسناد الكل' : 'Assign all' }),
    ).toBeDisabled();
    await expect(
      page.getByRole('button', { name: language === 'ar' ? 'إضافة رحلة' : 'Add trip' }).last(),
    ).toBeDisabled();
    await expect(tripRows.first()).toContainText(language === 'ar' ? 'غير مسندة' : 'Unassigned');
    await expect(tripRows.nth(1)).toContainText('Driver 3');
    await expect(tripRows.nth(1)).toContainText(language === 'ar' ? 'مكتملة' : 'Completed');
    await expect(tripRows.first()).not.toContainText('10:00 AM');
    const routeLines = await tripRows.first().evaluate((tripRow) => {
      const pickup = tripRow.querySelector('[data-trip-pickup]')!.getBoundingClientRect();
      const dropoff = tripRow.querySelector('[data-trip-dropoff]')!.getBoundingClientRect();
      return { pickupTop: pickup.top, dropoffTop: dropoff.top };
    });
    expect(routeLines.dropoffTop).toBeGreaterThan(routeLines.pickupTop);
    const tripLayout = await tripRows.evaluateAll((rows, direction) =>
      rows.map((button) => {
        const row = button.parentElement!;
        const badge = button.querySelector('[data-trip-status] [data-slot="badge"]')!;
        const badgeBox = badge.getBoundingClientRect();
        const statusBox = badge.parentElement!.getBoundingClientRect();
        const menuBox = row
          .querySelector('[data-slot="dropdown-menu-trigger"]')!
          .getBoundingClientRect();
        const rowBox = row.getBoundingClientRect();
        return {
          tracks: getComputedStyle(row).gridTemplateColumns,
          statusWidth: statusBox.width,
          badgeEnd: direction === 'rtl' ? badgeBox.left : badgeBox.right,
          statusEnd: direction === 'rtl' ? statusBox.left : statusBox.right,
          menuStart: direction === 'rtl' ? menuBox.right : menuBox.left,
          verticalOffset: Math.abs(
            (badgeBox.top + badgeBox.bottom - rowBox.top - rowBox.bottom) / 2,
          ),
        };
      }),
      language === 'ar' ? 'rtl' : 'ltr',
    );
    expect(tripLayout[0].tracks).toBe(tripLayout[1].tracks);
    expect(tripLayout[0].statusWidth).toBeGreaterThanOrEqual(100);
    expect(tripLayout[0].statusWidth).toBe(tripLayout[1].statusWidth);
    expect(Math.abs(tripLayout[0].statusEnd - tripLayout[1].statusEnd)).toBeLessThanOrEqual(1);
    for (const rowLayout of tripLayout) {
      expect(Math.abs(rowLayout.badgeEnd - rowLayout.statusEnd)).toBeLessThanOrEqual(1);
      expect(Math.abs(rowLayout.menuStart - rowLayout.statusEnd)).toBeLessThanOrEqual(10);
      expect(rowLayout.verticalOffset).toBeLessThanOrEqual(2);
    }
    await expect(tripRows.first().locator('..')).toHaveClass(/bg-accent/);
    await expect(
      page.locator('app-trip-detail ng-icon[name="lucideEllipsisVertical"]'),
    ).toHaveCount(2);
    const assignment = page.locator('[data-selected-trip-id]');
    await expect(assignment).toHaveAttribute('data-selected-trip-id', 'outbound');
    await page.locator('app-trip-detail button[data-slot="dropdown-menu-trigger"]').nth(1).click();
    const actions =
      language === 'ar'
        ? ['تعديل الرحلة', 'نقل إلى مجموعة رحلات أخرى', 'فصل في مجموعة مستقلة', 'حذف الرحلة']
        : ['Edit trip', 'Move to another journey', 'Separate into own journey', 'Delete trip'];
    for (const action of actions) {
      await expect(page.getByRole('menuitem', { name: action })).toBeDisabled();
    }
    await expect(tripRows.first()).toHaveAttribute('aria-pressed', 'true');
    await page.keyboard.press('Escape');
    await tripRows.nth(1).focus();
    await page.keyboard.press('Space');
    await expect(tripRows.nth(1)).toHaveAttribute('aria-pressed', 'true');
    await expect(assignment).toHaveAttribute('data-selected-trip-id', 'return');
    await expect(assignment).toContainText('Driver 3');
    await expect(assignment).toContainText('Van 2');
    await expect(tripRows.nth(1).locator('..')).toHaveClass(/bg-accent/);

    await page.setViewportSize({ width: 1440, height: 900 });
    await expect(header).toBeHidden();
    await page.setViewportSize({ width: 1100, height: 900 });
    await expect(header).toBeHidden();
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.locator('app-trip-detail')).toBeVisible();
    await expectDrawerTabsFit(page);
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1)).toBe(
      true,
    );
    await page.locator('app-trip-detail button:has(ng-icon[name="lucideX"])').click();
    await page.setViewportSize({ width: 1440, height: 900 });
    await expect(header).toBeVisible();
    await expectAlignedColumns(page, language === 'ar' ? 'rtl' : 'ltr');
  });
}
