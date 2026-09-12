import { routes } from './app.routes';

describe('application routes', () => {
  it('keeps the invitation acceptance URL out of the default redirect', () => {
    const invitationRoute = routes.find((route) => route.path === 'invitations/accept');

    expect(invitationRoute).toBeDefined();
    expect(invitationRoute?.redirectTo).toBeUndefined();
  });
});
