import { ApplicationError } from '../errors/application-error';
import type { UiFeedback } from '../i18n/ui-message';

export function httpErrorMessage(error: unknown, fallback: string): UiFeedback {
  if (!(error instanceof ApplicationError)) return { key: fallback };
  if (error.issues.length)
    return error.issues.map((issue) => ({ key: `errors.${issue.code}`, params: issue.parameters }));
  // A rejected form still needs actionable feedback when client and server validation differ.
  if (Object.keys(error.validationErrors).length) return { key: 'errors.validation' };
  const statusKeys: Record<number, string> = {
    0: 'errors.network',
    401: 'errors.unauthorized',
    403: 'errors.forbidden',
    404: 'errors.notFound',
    409: 'errors.conflict',
  };
  return { key: statusKeys[error.status] ?? fallback };
}
