import { HttpErrorResponse } from '@angular/common/http';

export class ApplicationError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly title?: string,
  ) {
    super(message);
  }
}

export function toApplicationError(error: unknown): ApplicationError {
  if (!(error instanceof HttpErrorResponse)) {
    return new ApplicationError('An unexpected error occurred.', 0);
  }

  const detail = error.error?.detail ?? error.error?.message;
  const message =
    typeof detail === 'string'
      ? detail
      : error.status === 0
        ? 'Network connection unavailable.'
        : 'The request could not be completed.';
  return new ApplicationError(message, error.status, error.error?.title);
}
