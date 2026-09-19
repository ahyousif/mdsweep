import { inject } from '@angular/core';
import { type CanActivateFn, Router } from '@angular/router';
import { AuthSessionService } from '@app/core/auth/auth-session.service';

export const vehiclesGuard: CanActivateFn = async () => {
  const auth = inject(AuthSessionService);
  const router = inject(Router);
  const session = await auth.establish();
  return (
    session.activeTenant?.roles.some((role) => role === 'Administrator' || role === 'Dispatcher') ||
    router.createUrlTree(['/trips'])
  );
};
