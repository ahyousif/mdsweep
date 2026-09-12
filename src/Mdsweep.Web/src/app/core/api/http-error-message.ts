import { ApplicationError } from '../errors/application-error';
import { UiMessage } from '../i18n/ui-message';

export function httpErrorMessage(error: unknown, fallback: string): UiMessage {
  if (!(error instanceof ApplicationError)) return { key: fallback };
  if (error.localizedErrors.length)
    return { key: 'errors.validation', details: error.localizedErrors };
  if (error.code) return { key: `errors.${error.code}`, params: error.parameters };
  const statusKeys: Record<number, string> = {
    0: 'errors.network',
    400: 'errors.validation',
    401: 'errors.unauthorized',
    403: 'errors.forbidden',
    404: 'errors.notFound',
    409: 'errors.conflict',
  };
  return { key: statusKeys[error.status] ?? fallback };
}
