import { HttpErrorResponse } from '@angular/common/http';

export function httpErrorMessage(error: unknown, fallback: string): string {
  return error instanceof HttpErrorResponse && typeof error.error?.message === 'string'
    ? error.error.message
    : fallback;
}
