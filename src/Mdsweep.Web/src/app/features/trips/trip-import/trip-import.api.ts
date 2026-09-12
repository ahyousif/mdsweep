import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { ApiClient } from '@app/core/api/api-client';

export type TripImportProblem = {
  rowNumber: number | null;
  tripNumber: string | null;
  field: string | null;
  message: string;
  code?: string;
  parameters?: Record<string, string | number>;
};

export type TripImportResult = {
  readyCount: number;
  needsAttentionCount: number;
  problems: TripImportProblem[];
};

@Injectable({ providedIn: 'root' })
export class TripImportApi {
  readonly #api = inject(ApiClient);

  import(file: File): Promise<TripImportResult> {
    const form = new FormData();

    form.append('file', file);

    return firstValueFrom(
      this.#api.http.post<TripImportResult>(this.#api.url('trips/import'), form),
    );
  }
}
