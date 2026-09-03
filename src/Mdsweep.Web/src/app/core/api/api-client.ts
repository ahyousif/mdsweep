import { HttpClient } from '@angular/common/http';
import { inject, Injectable, InjectionToken } from '@angular/core';

export const API_BASE_PATH = new InjectionToken<string>('API base path', {
  factory: () => '/api',
});

@Injectable({ providedIn: 'root' })
export class ApiClient {
  readonly http = inject(HttpClient);
  readonly #basePath = inject(API_BASE_PATH);

  url(path: string): string {
    return `${this.#basePath}/${path.replace(/^\/+/, '')}`;
  }
}
