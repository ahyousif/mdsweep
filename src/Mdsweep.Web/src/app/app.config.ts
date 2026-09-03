import {
  provideHttpClient,
  withInterceptors,
  withXhr,
  withXsrfConfiguration,
} from '@angular/common/http';
import {
  ApplicationConfig,
  ErrorHandler,
  isDevMode,
  provideBrowserGlobalErrorListeners,
  provideZoneChangeDetection,
} from '@angular/core';
import {
  provideRouter,
  withComponentInputBinding,
  withInMemoryScrolling,
  withViewTransitions,
} from '@angular/router';
import { provideServiceWorker } from '@angular/service-worker';
import { API_BASE_PATH } from '@app/core/api/api-client';
import { provideSpartanHlm } from '@spartan-ng/helm/utils';
import { provideTanStackQuery, QueryClient } from '@tanstack/angular-query-experimental';
import { routes } from './app.routes';
import { applicationErrorInterceptor } from './core/errors/application-error.interceptor';
import { GlobalErrorHandler } from './core/errors/global-error.handler';

export const appConfig: ApplicationConfig = {
  providers: [
    provideSpartanHlm(),
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(
      routes,
      withComponentInputBinding(),
      withInMemoryScrolling({
        scrollPositionRestoration: 'enabled',
        anchorScrolling: 'enabled',
      }),
      withViewTransitions(),
    ),
    provideHttpClient(
      withXhr(),
      withInterceptors([applicationErrorInterceptor]),
      withXsrfConfiguration({
        cookieName: 'XSRF-TOKEN',
        headerName: 'X-XSRF-TOKEN',
      }),
    ),
    { provide: API_BASE_PATH, useValue: '/api' },
    provideTanStackQuery(
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 30_000,
            retry: 1,
            refetchOnWindowFocus: false,
          },
          mutations: { retry: 0 },
        },
      }),
    ),
    { provide: ErrorHandler, useExisting: GlobalErrorHandler },
    provideServiceWorker('ngsw-worker.js', {
      enabled: !isDevMode(),
      registrationStrategy: 'registerWhenStable:30000',
    }),
  ],
};
