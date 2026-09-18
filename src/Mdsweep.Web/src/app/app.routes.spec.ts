import { routes } from './app.routes';

describe('application routes', () => {
  it('keeps the invitation acceptance URL out of the default redirect', () => {
    const invitationRoute = routes.find((route) => route.path === 'invitations/accept');

    expect(invitationRoute).toBeDefined();
    expect(invitationRoute?.redirectTo).toBeUndefined();
  });

  it('loads the Passenger workspace at its stable route', () => {
    expect(routes.find((route) => route.path === 'passengers')?.loadComponent).toBeDefined();
  });
});
