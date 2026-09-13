import { HttpErrorResponse } from '@angular/common/http';

export interface ApiIssue {
  field?: string;
  code: string;
  parameters?: Record<string, string | number>;
}

export class ApplicationError extends Error {
  constructor(
    message: string,
    readonly status: number,
    readonly title?: string,
    readonly validationErrors: Readonly<Record<string, readonly string[]>> = {},
    readonly issues: ApiIssue[] = [],
  ) {
    super(message);
  }
}

export function toApplicationError(error: unknown): ApplicationError {
  if (!(error instanceof HttpErrorResponse)) {
    return new ApplicationError('An unexpected error occurred.', 500);
  }

  const detail = error.error?.detail ?? error.error?.message;
  const validation = error.error?.errors;
  const validationErrors =
    validation && typeof validation === 'object'
      ? (validation as Record<string, readonly string[]>)
      : {};
  const validationMessage =
    validation && typeof validation === 'object'
      ? Object.values(validationErrors)
          .flat()
          .filter((value): value is string => typeof value === 'string')
          .join(' ')
      : '';
  const message =
    typeof detail === 'string'
      ? detail
      : validationMessage ||
        (error.status === 409
          ? 'This record changed. Refresh the page before trying again.'
          : error.status === 0
            ? 'Network connection unavailable.'
            : 'The request could not be completed.');
  const failures = error.error?.issues;
  const issues: ApiIssue[] = Array.isArray(failures)
    ? failures
        .filter((failure) => failure && typeof failure.code === 'string')
        .map((failure) => ({
          field: typeof failure.field === 'string' ? failure.field : undefined,
          code: failure.code,
          parameters: failure.parameters,
        }))
    : [];
  return new ApplicationError(message, error.status, error.error?.title, validationErrors, issues);
}
