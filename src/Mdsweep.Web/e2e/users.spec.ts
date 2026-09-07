import { expect, test, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

type Membership = {
  userId: string;
  firstName: string;
  lastName: string;
  tenantId: string;
  tenantName?: string;
  role: string;
};

function bootstrap(memberships: Membership[], activeTenantId?: string) {
  const availableTenants = [...new Set(memberships.map((x) => x.tenantId))].map((id) => ({
    id,
    name: memberships.find((x) => x.tenantId === id)!.tenantName ?? 'Synthetic Tenant',
    roles: memberships.filter((x) => x.tenantId === id).map((x) => x.role),
  }));
  return {
    userId: memberships[0]?.userId ?? null,
    displayName: memberships.length ? `${memberships[0].firstName} ${memberships[0].lastName}` : '',
    activeTenant:
      availableTenants.find((x) => x.id === activeTenantId) ??
      (activeTenantId === undefined && availableTenants.length === 1 ? availableTenants[0] : null),
    availableTenants,
  };
}

// Browser interactions use synthetic API responses. PostgreSQL HTTP tests verify
// authorization, acceptance, persistence, and history through the real API.
async function session(page: Page, role = 'Administrator') {
  await page.route('**/api/auth/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/session'))
      return route.fulfill({
        json: bootstrap([
          {
            userId: 'admin',
            firstName: 'Synthetic',
            lastName: 'Manager',
            tenantId: 'mdsw-eep2-3456',
            role,
          },
        ]),
      });
    if (path.endsWith('/antiforgery')) return route.fulfill({ json: { token: 'synthetic-token' } });
    return route.fulfill({ status: 204 });
  });
}

test('manage Users and recover from an invitation delivery failure', async ({ page }, testInfo) => {
  test.setTimeout(60_000);
  await session(page);
  let invitation: Record<string, unknown> | null = null;
  let user = {
    id: 'driver',
    firstName: 'Taylor',
    lastName: 'Example',
    email: 'taylor@example.test',
    roles: ['Driver'],
    isActive: true,
    version: 0,
  };
  await page.route('**/api/users**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === '/api/users')
      return route.fulfill({
        json: { users: [user], invitations: invitation ? [invitation] : [], isAdministrator: true },
      });
    if (path === '/api/users/invitations') {
      invitation = {
        ...request.postDataJSON(),
        id: 'invite',
        status: 'Pending',
        expiresAt: '2030-09-12T12:00:00Z',
        sentAt: null,
        deliveryError: 'Email delivery is not configured. Configure it and retry.',
        version: 1,
      };
      return route.fulfill({ status: 201, json: invitation });
    }
    if (path.endsWith('/resend')) {
      invitation = { ...invitation, sentAt: '2030-09-05T12:00:00Z', deliveryError: null };
      return route.fulfill({ json: invitation });
    }
    if (path.endsWith('/revoke')) {
      invitation = { ...invitation, status: 'Revoked' };
      return route.fulfill({ status: 204 });
    }
    if (path.endsWith('/history'))
      return route.fulfill({
        json: [
          {
            actorSubject: 'synthetic-manager',
            actorName: 'Synthetic Manager',
            action: 'Deactivated',
            occurredAt: '2030-09-05T12:00:00Z',
            details: null,
          },
        ],
      });
    if (request.method() === 'PUT') {
      user = { ...user, ...request.postDataJSON(), version: user.version + 1 };
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ status: 204 });
  });
  await page.goto('/users');
  await expect(page.getByRole('heading', { name: 'Users and Invitations' })).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.getByRole('button', { name: 'Invite User', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Invite User', exact: true })).toBeVisible();
  await expect(page.getByLabel('First name', { exact: true })).toBeFocused();
  await page.getByLabel('First name', { exact: true }).fill('Jordan');
  await page.getByLabel('Last name', { exact: true }).fill('Example');
  await page.getByLabel('Email', { exact: true }).fill('jordan@example.test');
  await page.getByRole('checkbox', { name: 'Dispatcher', exact: true }).click();
  await expect(page.getByRole('checkbox', { name: 'Dispatcher', exact: true })).toBeChecked();
  await expect(page.getByRole('checkbox', { name: 'Administrator', exact: true })).toBeDisabled();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.getByRole('button', { name: 'Send invitation', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('status')).toContainText('Email delivery failed');
  expect(invitation).toMatchObject({ roles: ['Driver', 'Dispatcher'] });
  await expect(
    page.getByText('Email delivery is not configured. Configure it and retry.'),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Resend invitation to Jordan Example' }).click();
  await expect(page.getByRole('status')).toHaveText('Invitation email sent.');
  await page.getByRole('button', { name: 'Revoke invitation for Jordan Example' }).click();
  await expect(page.getByRole('cell', { name: 'Revoked', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Edit Taylor Example', exact: true }).click();
  await page.getByLabel('First name', { exact: true }).fill('Taylor Updated');
  await page.getByRole('checkbox', { name: 'Dispatcher', exact: true }).click();
  await expect(page.getByRole('checkbox', { name: 'Dispatcher', exact: true })).toBeChecked();
  await page.getByRole('checkbox', { name: 'Active access', exact: true }).click();
  await expect(
    page.getByRole('checkbox', { name: 'Active access', exact: true }),
  ).not.toBeChecked();
  await page.getByRole('button', { name: 'Save changes', exact: true }).click();
  await expect(page.getByRole('cell', { name: 'Inactive', exact: true })).toBeVisible();
  expect(user.roles).toEqual(['Driver', 'Dispatcher']);
  await page.getByRole('button', { name: 'View history for Taylor Updated Example' }).click();
  await expect(page.getByText('Deactivated', { exact: true })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('users.png'), fullPage: true });
});

test('Dispatcher invitations offer only the Driver role', async ({ page }) => {
  await session(page, 'Dispatcher');
  await page.route('**/api/users', (route) =>
    route.fulfill({ json: { users: [], invitations: [], isAdministrator: false } }),
  );
  await page.goto('/users');
  await page.getByRole('button', { name: 'Invite User', exact: true }).click();
  await expect(page.getByRole('checkbox', { name: 'Driver', exact: true })).toBeChecked();
  await expect(page.getByRole('checkbox')).toHaveCount(1);
});

test('an authenticated invitee can accept without an existing Tenant Membership', async ({
  page,
}) => {
  let accepted = false;
  await page.route('**/api/auth/**', (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/session'))
      return route.fulfill({
        json: bootstrap(
          accepted
            ? [
                {
                  userId: 'driver',
                  firstName: 'Synthetic',
                  lastName: 'Driver',
                  tenantId: 'mdsw-eep2-3456',
                  role: 'Driver',
                },
              ]
            : [],
        ),
      });
    return route.fulfill({ json: { token: 'synthetic-token' } });
  });
  await page.route('**/api/invitation**', (route) => {
    if (route.request().method() === 'POST') {
      accepted = true;
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({
      json: accepted
        ? []
        : [
            {
              id: 'invite',
              tenantName: 'Synthetic Tenant',
              roles: ['Driver'],
              expiresAt: '2030-09-12T12:00:00Z',
            },
          ],
    });
  });
  await page.goto('/');
  await expect(
    page.getByText('You have been invited to Synthetic Tenant with roles: Driver.'),
  ).toBeVisible();
  await page
    .getByRole('button', { name: 'Accept invitation to Synthetic Tenant', exact: true })
    .click();
  await expect(
    page.getByRole('heading', { name: 'My Trips is not available in this build yet.' }),
  ).toBeVisible();
});

test('invitation dialog supports keyboard dismissal and preserves failed submissions', async ({
  page,
}, testInfo) => {
  await session(page);
  await page.route('**/api/users', (route) =>
    route.fulfill({
      json: { users: [], invitations: [], isAdministrator: true },
    }),
  );
  await page.route('**/api/users/invitations', (route) =>
    route.fulfill({
      status: 400,
      json: {
        errors: {
          email: ['An invitation already exists for this email. Resend or revoke it first.'],
        },
      },
    }),
  );
  await page.goto('/users');
  const trigger = page.getByRole('button', { name: 'Invite User', exact: true });
  const dialog = page.getByRole('dialog', { name: 'Invite User', exact: true });
  await trigger.click();
  await expect(dialog.getByLabel('First name', { exact: true })).toBeFocused();
  await page.keyboard.press('Escape');
  await expect(dialog).toHaveCount(0);
  await expect(trigger).toBeFocused();
  await trigger.click();
  await dialog.getByRole('button', { name: /^close$/i }).click();
  await expect(dialog).toHaveCount(0);
  await expect(trigger).toBeFocused();
  await page.setViewportSize({ width: 390, height: 640 });
  await trigger.click();
  await dialog.getByRole('button', { name: 'Send invitation', exact: true }).click();
  await expect(dialog.getByText('Enter a first name.', { exact: true })).toBeVisible();
  await expect(dialog.getByText('Enter a last name.', { exact: true })).toBeVisible();
  await expect(dialog.getByText('Enter a valid email address.', { exact: true })).toBeVisible();
  await expect(dialog.getByLabel('Email', { exact: true })).toHaveAttribute('aria-invalid', 'true');

  await dialog.getByLabel('First name', { exact: true }).fill('Jordan');
  await dialog.getByLabel('Last name', { exact: true }).fill('Example');
  await dialog.getByLabel('Email', { exact: true }).fill('jordan@example.test');
  await dialog.getByRole('button', { name: 'Send invitation', exact: true }).click();
  await expect(dialog.getByRole('alert')).toContainText('An invitation already exists');
  await expect(dialog.getByLabel('Email', { exact: true })).toHaveValue('jordan@example.test');
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('invite-dialog-mobile.png'), fullPage: true });
  await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await expect(trigger).toBeFocused();
  await trigger.click();
  await expect(dialog.getByLabel('Email', { exact: true })).toHaveValue('');
  await expect(dialog.getByRole('alert')).toHaveCount(0);
});

test('Users list errors recover to accessible empty results', async ({ page }) => {
  test.setTimeout(60_000);
  await session(page);
  let failing = true;
  await page.route('**/api/users', (route) =>
    route.fulfill(
      failing
        ? { status: 500, json: { detail: 'Users could not be loaded. Try again.' } }
        : { json: { users: [], invitations: [], isAdministrator: true } },
    ),
  );
  await page.goto('/users');
  await expect(page.getByRole('button', { name: 'Retry', exact: true })).toBeVisible({
    timeout: 20_000,
  });
  await expect(
    page.getByText('Users could not be loaded. Try again.', { exact: true }),
  ).toBeVisible();
  failing = false;
  await page.getByRole('button', { name: 'Retry', exact: true }).click();
  await page.getByRole('searchbox', { name: 'Search Users and Invitations' }).fill('Nobody');
  await expect(page.getByRole('heading', { name: 'No Users match.', exact: true })).toBeVisible();
  await expect(
    page.getByRole('heading', { name: 'No invitations match.', exact: true }),
  ).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

test('invitation acceptance shows unavailable and failed acceptance feedback', async ({
  page,
}, testInfo) => {
  await page.route('**/api/auth/**', (route) =>
    route.fulfill({
      json: route.request().url().endsWith('/session')
        ? bootstrap([])
        : { token: 'synthetic-token' },
    }),
  );
  let available = false;
  await page.route('**/api/invitation**', (route) =>
    route.fulfill(
      route.request().method() === 'POST'
        ? { status: 400, json: { detail: 'Open the invitation email and finish signup first.' } }
        : available
          ? {
              json: [
                {
                  id: 'invite',
                  tenantName: 'Synthetic Tenant',
                  roles: ['Driver'],
                  expiresAt: '2030-09-12T12:00:00Z',
                },
              ],
            }
          : {
              status: 503,
              json: { detail: 'Invitations are temporarily unavailable. Try again.' },
            },
    ),
  );
  await page.goto('/');
  await expect(page.getByRole('alert')).toContainText(
    'Invitations are temporarily unavailable. Try again.',
  );
  available = true;
  await page.getByRole('button', { name: 'Check again', exact: true }).click();
  await page
    .getByRole('button', { name: 'Accept invitation to Synthetic Tenant', exact: true })
    .click();
  await expect(page.getByRole('alert')).toHaveText(
    'Open the invitation email and finish signup first.',
  );
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('invitation-feedback.png'), fullPage: true });
});

test('multiple Tenant access can be selected and switched without mixing Users', async ({
  page,
}) => {
  let activeTenant = '';
  const tenants = [
    { tenantId: 'mdsw-eep2-3456', tenantName: 'Alpha Synthetic Tenant', role: 'Administrator' },
    { tenantId: 'abcd-efgh-jkmn', tenantName: 'Beta Synthetic Tenant', role: 'Dispatcher' },
  ];
  await page.route('**/api/auth/**', (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/session'))
      return route.fulfill({
        json: bootstrap(
          tenants.map((tenant) => ({
            userId: 'same-user',
            firstName: 'Synthetic',
            lastName: 'Manager',
            ...tenant,
          })),
          activeTenant,
        ),
      });
    if (path.endsWith('/tenant-context')) activeTenant = route.request().postDataJSON().tenantId;
    return route.fulfill({ json: { token: 'synthetic-token' } });
  });
  await page.route('**/api/invitation', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/users', (route) =>
    route.fulfill({
      json: {
        users: [
          {
            id: 'tenant-user',
            firstName: activeTenant === tenants[0].tenantId ? 'Alpha' : 'Beta',
            lastName: 'Driver',
            email: 'synthetic@example.test',
            roles: ['Driver'],
            isActive: true,
            version: 0,
          },
        ],
        invitations: [],
        isAdministrator: activeTenant === tenants[0].tenantId,
      },
    }),
  );
  await page.goto('/users');
  await expect(page.getByRole('heading', { name: 'Tenants and Invitations' })).toBeVisible();
  await page.getByRole('button', { name: 'Open Alpha Synthetic Tenant' }).click();
  await expect(page.getByRole('button', { name: 'Open user menu' })).toBeVisible();
  await page.goto('/users');
  await expect(page.getByRole('cell', { name: 'Alpha Driver', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Invite User', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByRole('combobox')).toHaveCount(0);
  await expect(dialog.getByText('Tenant', { exact: true })).toHaveCount(0);
  await dialog.getByRole('button', { name: 'Cancel' }).click();
  await page.getByRole('button', { name: 'Open user menu' }).click();
  await page.getByRole('menuitem', { name: 'Tenants and Invitations' }).click();
  await page.getByRole('button', { name: 'Open Beta Synthetic Tenant' }).click();
  await expect(page.getByRole('button', { name: 'Open user menu' })).toBeVisible();
  await page.goto('/users');
  await expect(page.getByRole('cell', { name: 'Beta Driver', exact: true })).toBeVisible();
  await expect(page.getByRole('cell', { name: 'Alpha Driver', exact: true })).toHaveCount(0);
  expect(activeTenant).toBe(tenants[1].tenantId);
  await page.getByRole('button', { name: 'Invite User', exact: true }).click();
  await expect(page.getByRole('checkbox')).toHaveCount(1);
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

test('existing Users can see and accept another Tenant invitation from their menu', async ({
  page,
}, testInfo) => {
  let accepted = false;
  await page.route('**/api/auth/**', (route) => {
    if (route.request().url().endsWith('/session'))
      return route.fulfill({
        json: bootstrap(
          [
            {
              userId: 'same-user',
              firstName: 'Synthetic',
              lastName: 'Manager',
              tenantId: 'mdsw-eep2-3456',
              tenantName: 'Alpha Synthetic Tenant',
              role: 'Administrator',
            },
            ...(accepted
              ? [
                  {
                    userId: 'same-user',
                    firstName: 'Synthetic',
                    lastName: 'Manager',
                    tenantId: 'abcd-efgh-jkmn',
                    tenantName: 'Beta Synthetic Tenant',
                    role: 'Driver',
                  },
                ]
              : []),
          ],
          'mdsw-eep2-3456',
        ),
      });
    return route.fulfill({ json: { token: 'synthetic-token' } });
  });
  await page.route('**/api/users', (route) =>
    route.fulfill({ json: { users: [], invitations: [], isAdministrator: true } }),
  );
  await page.route('**/api/invitation**', (route) => {
    if (route.request().method() === 'POST') {
      accepted = true;
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({
      json: accepted
        ? []
        : [
            {
              id: 'second-invite',
              tenantName: 'Beta Synthetic Tenant',
              roles: ['Driver'],
              expiresAt: '2030-09-12T12:00:00Z',
            },
          ],
    });
  });
  await page.goto('/users');
  await page.getByRole('button', { name: 'Open user menu' }).click();
  await page.getByRole('menuitem', { name: 'Tenants and Invitations' }).click();
  await expect(page.getByRole('button', { name: 'Open Alpha Synthetic Tenant' })).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath('multiple-tenant-access.png'),
    fullPage: true,
  });
  await page.getByRole('button', { name: 'Accept invitation to Beta Synthetic Tenant' }).click();
  await expect(page.getByRole('button', { name: 'Open Beta Synthetic Tenant' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Open Alpha Synthetic Tenant' })).toBeVisible();
  await expect(page.getByText('No pending invitations.', { exact: false })).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

test('switching Tenant reloads another open tab sharing the BFF cookie', async ({ context }) => {
  const tenants = [
    { tenantId: 'mdsw-eep2-3456', tenantName: 'Alpha Tenant' },
    { tenantId: 'abcd-efgh-jkmn', tenantName: 'Beta Tenant' },
  ];
  let activeTenant = '';
  await context.route('**/api/auth/**', (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/session'))
      return route.fulfill({
        json: bootstrap(
          tenants.map((tenant) => ({
            userId: 'same-user',
            firstName: 'Synthetic',
            lastName: 'Manager',
            role: 'Administrator',
            ...tenant,
          })),
          activeTenant,
        ),
      });
    if (path.endsWith('/tenant-context')) activeTenant = route.request().postDataJSON().tenantId;
    return route.fulfill({ json: { token: 'synthetic-token' } });
  });
  await context.route('**/api/invitation', (route) => route.fulfill({ json: [] }));
  await context.route('**/api/users', (route) =>
    route.fulfill({
      json: {
        users: [
          {
            id: 'tenant-user',
            firstName: activeTenant === tenants[0].tenantId ? 'Alpha' : 'Beta',
            lastName: 'User',
            email: 'synthetic@example.test',
            roles: ['Driver'],
            isActive: true,
            version: 0,
          },
        ],
        invitations: [],
        isAdministrator: true,
      },
    }),
  );
  const first = await context.newPage();
  await first.goto('/users');
  await first.getByRole('button', { name: 'Open Alpha Tenant' }).click();
  await expect(first.getByRole('button', { name: 'Open user menu' })).toBeVisible();
  const second = await context.newPage();
  await second.goto('/users');
  await expect(second.getByRole('cell', { name: 'Alpha User', exact: true })).toBeVisible();
  await first.getByRole('button', { name: 'Open user menu' }).click();
  await first.getByRole('menuitem', { name: 'Tenants and Invitations' }).click();
  await first.getByRole('button', { name: 'Open Beta Tenant' }).click();
  await expect(second.getByRole('cell', { name: 'Beta User', exact: true })).toBeVisible();
  await expect(second.getByRole('cell', { name: 'Alpha User', exact: true })).toHaveCount(0);
});
