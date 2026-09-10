import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiClient } from '@app/core/api/api-client';

import { TripImportSummary } from './trip-import.types';

@Injectable({
  providedIn: 'root',
})
export default class TripImportApi {
  readonly #api = inject(ApiClient);

  preview(file: File): Promise<TripImportSummary> {
    return this.upload('trips/import/preview', file);
  }

  import(file: File): Promise<TripImportSummary> {
    return this.upload('trips/import', file);
  }

  private upload(path: string, file: File): Promise<TripImportSummary> {
    const form = new FormData();

    form.append('file', file);

    return firstValueFrom(this.#api.http.post<TripImportSummary>(this.#api.url(path), form));
  }
}
