import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiClient } from '@app/core/api/api-client';

export type TripImportProblem = {
  rowNumber: number;
  tripNumber: string | null;
  message: string;
};

export type TripImportResult = {
  fileName: string;
  total: number;
  added: number;
  updated: number;
  unchanged: number;
  needsAttention: number;
  problems: TripImportProblem[];
};

@Injectable({ providedIn: 'root' })
export class TripImportApi {
  private readonly api = inject(ApiClient);

  import(file: File): Promise<TripImportResult> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.api.http.post<TripImportResult>(this.api.url('trips/import'), form));
  }
}
