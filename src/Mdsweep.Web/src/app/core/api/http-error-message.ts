import { ApplicationError } from '../errors/application-error';

export function httpErrorMessage(error: unknown, fallback: string): string {
  return error instanceof ApplicationError ? error.message : fallback;
}
