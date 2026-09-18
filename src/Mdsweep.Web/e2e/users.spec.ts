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

type UserListItemFixture = {
  id: string;
  type: 'User' | 'Invitation';
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  roles: string[];
  status: 'Active' | 'Inactive' | 'Invited';
  expiresAt?: string | null;
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
// authorization, acceptance, and persistence through the real API.
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

test('manages active, invited, and disabled users in one selectable list', async ({
  page,
}, testInfo) => {
  test.setTimeout(60_000);
  await session(page);
  let users: UserListItemFixture[] = [
    {
      id: 'invite-existing',
      type: 'Invitation',
      displayName: 'Omar Hassan',
      firstName: 'Omar',
      lastName: 'Hassan',
      email: 'omar@example.test',
      roles: ['Dispatcher'],
      status: 'Invited',
      expiresAt: '2030-09-18T12:00:00Z',
    },
    {
      id: 'disabled',
      type: 'User',
      displayName: 'Casey Disabled',
      firstName: 'Casey',
      lastName: 'Disabled',
      email: 'casey@example.test',
      roles: ['Driver'],
      status: 'Inactive',
    },
  ];
  let listRequests = 0;
  let user: UserListItemFixture = {
    id: 'driver',
    type: 'User',
    displayName: 'Taylor Example',
    firstName: 'Taylor',
    lastName: 'Example',
    email: 'taylor@example.test',
    roles: ['Driver'],
    status: 'Active',
  };
  await page.route('**/api/users**', async (route) => {
    const request = route.request();
    const path = new URL(request.url()).pathname;
    if (path === '/api/users' && request.method() === 'GET') {
      listRequests += 1;
      return route.fulfill({ json: [user, ...users] });
    }
    if (path === '/api/users/invitations' && request.method() === 'POST') {
      const invitation: UserListItemFixture = {
        ...request.postDataJSON(),
        id: 'invite',
        type: 'Invitation',
        displayName: 'Jordan Example',
        status: 'Invited',
        expiresAt: '2030-09-18T12:00:00Z',
      };
      users = [...users, invitation];
      return route.fulfill({ status: 204 });
    }
    if (path.endsWith('/resend') && request.method() === 'POST') {
      return route.fulfill({ status: 204 });
    }
    if (request.method() === 'DELETE') {
      const id = path.split('/').at(-1);
      users = users.filter((item) => item.id !== id);
      return route.fulfill({ status: 204 });
    }
    if (request.method() === 'PUT') {
      const update = request.postDataJSON();
      user = {
        ...user,
        displayName: update.displayName,
        roles: update.roles,
        status: update.isActive ? 'Active' : 'Inactive',
      };
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({ status: 204 });
  });
  await page.goto('/users');
  await expect(page.getByRole('heading', { name: 'Users', exact: true })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Actions' })).toHaveCount(0);
  await expect(page.getByRole('row', { name: /Taylor Example/ })).toBeVisible();
  await expect(page.getByRole('row', { name: /Omar Hassan/ })).toContainText('Invited');
  await expect(page.getByRole('row', { name: /Casey Disabled/ })).toContainText('Disabled');

  await page.getByRole('searchbox', { name: 'Search users' }).fill('omar@example.test');
  await expect(page.getByRole('row', { name: /Omar Hassan/ })).toBeVisible();
  await expect(page.getByRole('row', { name: /Taylor Example/ })).toHaveCount(0);
  await page.getByRole('searchbox', { name: 'Search users' }).fill('');

  await page.getByRole('button', { name: /Active 1/ }).click();
  await expect(page.getByRole('row', { name: /Taylor Example/ })).toBeVisible();
  await expect(page.getByRole('row', { name: /Omar Hassan/ })).toHaveCount(0);
  await page.getByRole('button', { name: /Invited 1/ }).click();
  await expect(page.getByRole('row', { name: /Omar Hassan/ })).toBeVisible();
  await expect(page.getByRole('row', { name: /Casey Disabled/ })).toHaveCount(0);
  await page.getByRole('button', { name: /Disabled 1/ }).click();
  await expect(page.getByRole('row', { name: /Casey Disabled/ })).toBeVisible();
  await expect(page.getByRole('row', { name: /Taylor Example/ })).toHaveCount(0);
  await page.getByRole('row', { name: /Casey Disabled/ }).click();
  const detailPanel = page.locator('aside');
  await expect(detailPanel.getByRole('heading', { name: 'Details' })).toBeVisible();
  await expect(detailPanel.locator('header').getByText('Disabled', { exact: true })).toBeVisible();
  await expect(detailPanel.locator('dl').getByText('Disabled', { exact: true })).toHaveCount(0);
  await expect(detailPanel.getByRole('button', { name: 'Enable', exact: true })).toBeVisible();
  await expect(detailPanel.getByRole('button', { name: 'User actions' })).toHaveCount(0);
  await page.getByRole('button', { name: /All 3/ }).click();

  const invitedRow = page.getByRole('row', { name: /Omar Hassan/ });
  await invitedRow.click();
  await expect(invitedRow).toHaveAttribute('aria-selected', 'true');
  await expect(detailPanel.getByText('Expires', { exact: true })).toBeVisible();
  await detailPanel.getByRole('button', { name: 'Invitation actions' }).click();
  await expect(page.getByRole('menuitem', { name: 'Resend invitation' })).toBeVisible();
  await expect(page.getByRole('menuitem', { name: 'Cancel invitation' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Edit', exact: true })).toHaveCount(0);
  await page.getByRole('menuitem', { name: 'Resend invitation' }).click();
  await expect(page.getByRole('status')).toHaveText('Invitation resent to omar@example.test');
  await expect(invitedRow).toHaveAttribute('aria-selected', 'true');

  const activeRow = page.getByRole('row', { name: /Taylor Example/ });
  await activeRow.click();
  await expect(activeRow).toHaveAttribute('aria-selected', 'true');
  await expect(detailPanel.getByRole('button', { name: 'Close user details' })).toBeVisible();
  await expect(detailPanel.getByRole('button', { name: 'Close user details' })).not.toHaveClass(/text-destructive/);
  await expect(page.getByRole('menuitem', { name: 'Cancel invitation' })).toHaveCount(0);
  await expect(detailPanel.getByRole('heading', { name: 'Details', exact: true })).toBeVisible();
  await expect(detailPanel.getByText('Roles', { exact: true })).toBeVisible();
  await expect(detailPanel.locator('dl').getByText('Status', { exact: true })).toHaveCount(0);
  await expect(detailPanel.locator('dl [data-slot="badge"]')).toHaveCount(0);
  await expect(detailPanel.getByRole('button', { name: 'Disable', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Edit', exact: true }).click();
  await expect(detailPanel.getByRole('heading', { name: 'Edit user' })).toBeVisible();
  await expect(detailPanel.getByRole('checkbox', { name: 'Active access' })).toHaveCount(0);
  await detailPanel.getByRole('button', { name: 'Back to details' }).click();
  await expect(activeRow).toHaveAttribute('aria-selected', 'true');
  await page.getByRole('button', { name: 'Edit', exact: true }).click();
  await page.getByRole('checkbox', { name: 'Dispatcher', exact: true }).click();
  await page.getByRole('button', { name: 'Save changes', exact: true }).click();
  await expect(activeRow).toContainText('Dispatcher');
  expect(user.roles).toEqual(['Driver', 'Dispatcher']);
  await expect(detailPanel.locator('dl').locator('dd').filter({ hasText: 'Driver, Dispatcher' })).toBeVisible();
  await detailPanel.getByRole('button', { name: 'Disable', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Disable user?' })).toBeVisible();
  await page.getByRole('dialog').getByRole('button', { name: 'Disable user' }).click();
  await expect(activeRow).toContainText('Disabled');
  await detailPanel.getByRole('button', { name: 'Enable', exact: true }).click();
  await expect(activeRow).toContainText('Active');

  await page.setViewportSize({ width: 390, height: 700 });
  await expect(detailPanel.getByRole('button', { name: 'Close user details' })).toHaveCount(0);
  await expect(detailPanel.getByRole('button', { name: 'Back to users' })).toBeVisible();
  await detailPanel.getByRole('button', { name: 'Edit' }).click();
  await expect(detailPanel.getByRole('heading', { name: 'Edit user' })).toBeVisible();
  await detailPanel.getByRole('button', { name: 'Back to details' }).click();
  await detailPanel.getByRole('button', { name: 'Back to users' }).click();
  await expect(detailPanel).toHaveCount(0);
  await page.setViewportSize({ width: 1280, height: 800 });

  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.getByRole('button', { name: /Active 1/ }).click();
  await page.getByRole('searchbox', { name: 'Search users' }).fill('Taylor');
  const listRequestsBeforeInvite = listRequests;
  await page.getByRole('button', { name: 'Invite user', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Invite user', exact: true })).toBeVisible();
  await expect(page.getByLabel('First name', { exact: true })).toBeFocused();
  await page.getByLabel('First name', { exact: true }).fill('Jordan');
  await page.getByLabel('Last name', { exact: true }).fill('Example');
  await page.getByLabel('Email', { exact: true }).fill('jordan@example.test');
  await page.getByRole('checkbox', { name: 'Dispatcher', exact: true }).click();
  await expect(page.getByRole('checkbox', { name: 'Dispatcher', exact: true })).toBeChecked();
  await expect(page.getByRole('checkbox', { name: 'Administrator', exact: true })).toBeEnabled();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.getByRole('button', { name: 'Send invitation', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('status')).toHaveText('Invitation sent to jordan@example.test');
  await expect(page.getByRole('button', { name: /Active 1/ })).toHaveAttribute(
    'aria-pressed',
    'true',
  );
  await expect(page.getByRole('searchbox', { name: 'Search users' })).toHaveValue('Taylor');
  await expect.poll(() => listRequests).toBeGreaterThan(listRequestsBeforeInvite);
  const newInvitation = page.getByRole('row', { name: /Jordan Example/ });
  await expect(newInvitation).toHaveCount(0);

  await page.getByRole('searchbox', { name: 'Search users' }).fill('');
  await page.getByRole('button', { name: /Invited 2/ }).click();
  await expect(newInvitation).toContainText('Invited');
  await newInvitation.click();
  await detailPanel.getByRole('button', { name: 'Invitation actions' }).click();
  await page.getByRole('menuitem', { name: 'Cancel invitation' }).click();
  await expect(newInvitation).toHaveCount(0);
  await expect(detailPanel).toHaveCount(0);
  await page.screenshot({ path: testInfo.outputPath('users.png'), fullPage: true });
});

for (const language of ['en', 'ar'] as const) {
  const labels =
    language === 'en'
      ? {
          invite: 'Invite user',
          first: 'First name',
          last: 'Last name',
          email: 'Email',
          send: 'Send invitation',
          edit: 'Edit',
          save: 'Save changes',
          driver: 'Driver',
          dispatcher: 'Dispatcher',
          admin: 'Administrator',
          required: 'Choose at least one role.',
        }
      : {
          invite: 'دعوة مستخدم',
          first: 'الاسم الأول',
          last: 'اسم العائلة',
          email: 'البريد الإلكتروني',
          send: 'إرسال الدعوة',
          edit: 'تعديل',
          save: 'حفظ التغييرات',
          driver: 'سائق',
          dispatcher: 'منسق الرحلات',
          admin: 'مسؤول النظام',
          required: 'اختر دورًا واحدًا على الأقل.',
        };

  test(`Administrator can clear and select all invitation roles in ${language}`, async ({
    page,
  }) => {
    await page.addInitScript(
      (language) => localStorage.setItem('mdsweep.language', language),
      language,
    );
    await session(page);
    await page.route('**/api/users', (route) => route.fulfill({ json: [] }));
    let submittedRoles: string[] = [];
    await page.route('**/api/users/invitations', (route) => {
      submittedRoles = route.request().postDataJSON().roles;
      return route.fulfill({ status: 204 });
    });
    await page.goto('/users');
    await page.getByRole('button', { name: labels.invite, exact: true }).click();
    const dialog = page.getByRole('dialog');
    const driver = dialog.getByRole('checkbox', { name: labels.driver, exact: true });
    const error = dialog.getByRole('alert').filter({ hasText: labels.required });
    await expect(driver).toBeChecked();
    await driver.click();
    await expect(error).toBeVisible();
    for (const checkbox of await dialog.getByRole('checkbox').all()) {
      await expect(checkbox).toBeEnabled();
      await expect(checkbox).not.toBeChecked();
      await checkbox.click();
    }
    await expect(error).toHaveCount(0);
    for (const checkbox of await dialog.getByRole('checkbox').all())
      await expect(checkbox).toBeChecked();
    await dialog.getByLabel(labels.first, { exact: true }).fill('Synthetic');
    await dialog.getByLabel(labels.last, { exact: true }).fill('Invitee');
    await dialog.getByLabel(labels.email, { exact: true }).fill('all-roles@example.test');
    await dialog.getByRole('button', { name: labels.send, exact: true }).click();
    await expect(dialog).toHaveCount(0);
    expect(submittedRoles.sort()).toEqual(['Administrator', 'Dispatcher', 'Driver']);
  });

  test(`Administrator can save and reopen all three User roles in ${language}`, async ({
    page,
  }) => {
    await page.addInitScript(
      (language) => localStorage.setItem('mdsweep.language', language),
      language,
    );
    await session(page);
    const user: UserListItemFixture = {
      id: 'synthetic-user',
      type: 'User',
      firstName: 'Synthetic',
      lastName: 'User',
      displayName: 'Synthetic User',
      email: 'all-roles@example.test',
      roles: ['Driver'],
      status: 'Active',
    };
    await page.route('**/api/users', (route) => route.fulfill({ json: [user] }));
    let submittedRoles: string[] = [];
    await page.route('**/api/users/synthetic-user', (route) => {
      submittedRoles = route.request().postDataJSON().roles;
      user.roles = submittedRoles;
      return route.fulfill({ status: 204 });
    });
    await page.goto('/users');
    await page.getByRole('row').filter({ hasText: user.email }).click();
    await page.getByRole('button', { name: labels.edit, exact: true }).click();
    await page.getByRole('checkbox', { name: labels.dispatcher, exact: true }).click();
    await page.getByRole('checkbox', { name: labels.admin, exact: true }).click();
    await page.getByRole('button', { name: labels.save, exact: true }).click();
    await expect.poll(() => submittedRoles).toHaveLength(3);
    expect(submittedRoles.sort()).toEqual(['Administrator', 'Dispatcher', 'Driver']);
    await expect(page.getByRole('row').filter({ hasText: user.email })).toContainText(labels.admin);
    const roleValue = page.locator('aside dl > div')
      .filter({ has: page.locator('dt').filter({ hasText: language === 'en' ? /^Roles$/ : /^الأدوار$/ }) })
      .locator('dd');
    await expect(roleValue).toContainText(labels.admin);
    await expect(roleValue).toContainText(language === 'en' ? ',' : '،');
    await expect(roleValue.locator('[data-slot="badge"]')).toHaveCount(0);
    await page.reload();
    await page.getByRole('row').filter({ hasText: user.email }).click();
    await page.getByRole('button', { name: labels.edit, exact: true }).click();
    for (const name of [labels.driver, labels.dispatcher, labels.admin]) {
      await expect(page.getByRole('checkbox', { name, exact: true })).toBeChecked();
    }
  });
}

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
  await page.route('**/api/users/invitations/accept', (route) => {
    expect(route.request().postDataJSON()).toEqual({ token: 'raw-invitation-token' });
    accepted = true;
    return route.fulfill({ status: 204 });
  });
  await page.goto('/invitations/accept?token=raw-invitation-token');
  await expect(page.getByText('Accept this invitation to add its Tenant access')).toBeVisible();
  await page.getByRole('button', { name: 'Accept invitation', exact: true }).click();
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
      json: [],
    }),
  );
  await page.route('**/api/users/invitations', (route) =>
    route.fulfill({
      status: 400,
      json: {
        errors: {
          email: ['This user already belongs to this Tenant.'],
        },
        issues: [{ field: 'email', code: 'membershipExists' }],
      },
    }),
  );
  await page.goto('/users');
  const trigger = page.getByRole('button', { name: 'Invite user', exact: true });
  const dialog = page.getByRole('dialog', { name: 'Invite user', exact: true });
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
  await expect(dialog.getByRole('alert')).toContainText('already belongs to this Tenant');
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
        : { json: [] },
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
  await page.getByRole('searchbox', { name: 'Search users' }).fill('Nobody');
  await expect(page.getByRole('heading', { name: 'No users match.', exact: true })).toBeVisible();
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

test('invitation acceptance shows failed token feedback', async ({ page }, testInfo) => {
  await page.route('**/api/auth/**', (route) =>
    route.fulfill({
      json: route.request().url().endsWith('/session')
        ? bootstrap([])
        : { token: 'synthetic-token' },
    }),
  );
  await page.route('**/api/users/invitations/accept', (route) =>
    route.fulfill({
      status: 400,
      json: {
        errors: { token: ['Invalid invitation'] },
        issues: [{ field: 'token', code: 'invitationInvalid' }],
      },
    }),
  );
  await page.goto('/invitations/accept?token=invalid-token');
  await page.getByRole('button', { name: 'Accept invitation', exact: true }).click();
  await expect(page.getByRole('alert')).toHaveText(
    'This invitation is invalid, expired, cancelled, or already used.',
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
  await page.route('**/api/trips**', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/users', (route) =>
    route.fulfill({
      json: [
        {
          id: 'tenant-user',
          type: 'User',
          displayName: activeTenant === tenants[0].tenantId ? 'Alpha Driver' : 'Beta Driver',
          firstName: 'Synthetic',
          lastName: 'Driver',
          email: 'synthetic@example.test',
          roles: ['Driver'],
          status: 'Active',
        },
      ],
    }),
  );
  await page.goto('/users');
  await expect(page.getByRole('heading', { name: 'Tenant access' })).toBeVisible();
  await page.getByRole('button', { name: 'Open Alpha Synthetic Tenant' }).click();
  await expect(page.getByRole('button', { name: 'Open user menu' })).toBeVisible();
  await page.goto('/users');
  await expect(page.getByRole('row', { name: /Alpha Driver/ })).toBeVisible();
  await page.getByRole('button', { name: 'Invite user', exact: true }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByRole('combobox')).toHaveCount(0);
  await expect(dialog.getByText('Tenant', { exact: true })).toHaveCount(0);
  await dialog.getByRole('button', { name: 'Cancel' }).click();
  await page.getByRole('button', { name: 'Open user menu' }).click();
  await page.getByRole('menuitem', { name: 'Tenant access' }).click();
  await page.getByRole('button', { name: 'Open Beta Synthetic Tenant' }).click();
  await expect(page.getByRole('button', { name: 'Open user menu' })).toBeVisible();
  await page.goto('/users');
  await expect(page).toHaveURL(/\/trips$/);
  await expect(page.getByRole('link', { name: 'Users' })).toHaveCount(0);
  await expect(page.getByRole('row', { name: /Alpha Driver/ })).toHaveCount(0);
  expect(activeTenant).toBe(tenants[1].tenantId);
  await expect(page.getByRole('button', { name: 'Invite user', exact: true })).toHaveCount(0);
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
});

test('existing Users can accept another Tenant invitation from its secure link', async ({
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
  await page.route('**/api/users', (route) => route.fulfill({ json: [] }));
  await page.route('**/api/users/invitations/accept', (route) => {
    expect(route.request().postDataJSON()).toEqual({ token: 'second-tenant-token' });
    accepted = true;
    return route.fulfill({ status: 204 });
  });
  await page.goto('/invitations/accept?token=second-tenant-token');
  await expect(page.getByRole('button', { name: 'Open Alpha Synthetic Tenant' })).toBeVisible();
  await page.screenshot({
    path: testInfo.outputPath('multiple-tenant-access.png'),
    fullPage: true,
  });
  await page.getByRole('button', { name: 'Accept invitation', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Open user menu' })).toBeVisible();
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
  await context.route('**/api/users', (route) =>
    route.fulfill({
      json: [
        {
          id: 'tenant-user',
          type: 'User',
          displayName: activeTenant === tenants[0].tenantId ? 'Alpha User' : 'Beta User',
          firstName: 'Synthetic',
          lastName: 'User',
          email: 'synthetic@example.test',
          roles: ['Driver'],
          status: 'Active',
        },
      ],
    }),
  );
  const first = await context.newPage();
  await first.goto('/users');
  await first.getByRole('button', { name: 'Open Alpha Tenant' }).click();
  await expect(first.getByRole('button', { name: 'Open user menu' })).toBeVisible();
  const second = await context.newPage();
  await second.goto('/users');
  await expect(second.getByRole('row', { name: /Alpha User/ })).toBeVisible();
  await first.getByRole('button', { name: 'Open user menu' }).click();
  await first.getByRole('menuitem', { name: 'Tenant access' }).click();
  await first.getByRole('button', { name: 'Open Beta Tenant' }).click();
  await expect(second.getByRole('row', { name: /Beta User/ })).toBeVisible();
  await expect(second.getByRole('row', { name: /Alpha User/ })).toHaveCount(0);
});
