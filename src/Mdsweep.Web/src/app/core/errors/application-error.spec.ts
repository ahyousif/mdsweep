import { HttpErrorResponse } from '@angular/common/http';
import { toApplicationError } from './application-error';

describe('application errors', () => {
  it('shows validation details so the User can fix a rejected change', () => {
    const error = toApplicationError(
      new HttpErrorResponse({
        status: 400,
        error: {
          errors: { email: ['An invitation already exists. Resend it instead.'] },
        },
      }),
    );
    expect(error.message).toBe('An invitation already exists. Resend it instead.');
  });
  it('provides recovery for a stale version', () => {
    expect(toApplicationError(new HttpErrorResponse({ status: 409 })).message).toContain('Refresh');
  });
});
