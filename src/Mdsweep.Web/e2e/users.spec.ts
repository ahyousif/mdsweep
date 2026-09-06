import { expect, test, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';

// Browser interactions use synthetic API responses. PostgreSQL HTTP tests verify
// authorization, acceptance, persistence, and history through the real API.
async function session(page: Page, role = 'Administrator') {
  await page.route('**/api/auth/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    if (path.endsWith('/me'))
      return route.fulfill({
        json: [
          {
            userId: 'admin',
            firstName: 'Synthetic',
            lastName: 'Manager',
            tenantId: 'mdsw-eep2-3456',
            role,
          },
        ],
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
    if (path.endsWith('/me'))
      return route.fulfill({
        json: accepted
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
      });
    return route.fulfill({ json: { token: 'synthetic-token' } });
  });
  await page.route('**/api/invitation**', (route) => {
    if (route.request().method() === 'POST') {
      accepted = true;
      return route.fulfill({ status: 204 });
    }
    return route.fulfill({
      json: {
        id: 'invite',
        tenantName: 'Synthetic Tenant',
        roles: ['Driver'],
        expiresAt: '2030-09-12T12:00:00Z',
      },
    });
  });
  await page.goto('/');
  await expect(
    page.getByText('You have been invited to Synthetic Tenant with roles: Driver.'),
  ).toBeVisible();
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
      json: route.request().url().endsWith('/me') ? [] : { token: 'synthetic-token' },
    }),
  );
  let available = false;
  await page.route('**/api/invitation**', (route) =>
    route.fulfill(
      route.request().method() === 'POST'
        ? { status: 400, json: { detail: 'Open the invitation email and finish signup first.' } }
        : available
          ? {
              json: {
                id: 'invite',
                tenantName: 'Synthetic Tenant',
                roles: ['Driver'],
                expiresAt: '2030-09-12T12:00:00Z',
              },
            }
          : { status: 404, json: {} },
    ),
  );
  await page.goto('/');
  await expect(page.getByRole('alert')).toContainText('There is no active invitation');
  available = true;
  await page.getByRole('button', { name: 'Check again', exact: true }).click();
  await page.getByRole('button', { name: 'Accept invitation', exact: true }).click();
  await expect(page.getByRole('alert')).toHaveText(
    'Open the invitation email and finish signup first.',
  );
  expect((await new AxeBuilder({ page }).analyze()).violations).toEqual([]);
  await page.screenshot({ path: testInfo.outputPath('invitation-feedback.png'), fullPage: true });
});
