import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type TripImportItem = {
  rowNumber: number;
  tripNumber: string;
  brokerMemberId: string | null;
  disposition: 'Ready' | 'Warning' | 'Blocked' | string;
  messages: string[];
  serviceDate: string | null;
  appointmentTime: string | null;
};

export type TripImport = {
  id: string;
  fileName: string;
  status: string;
  appliedAt: string | null;
  items: TripImportItem[];
};

export function tripImportDispositionCounts(items: TripImportItem[]) {
  return {
    ready: items.filter((item) => item.disposition === 'Ready').length,
    warning: items.filter((item) => item.disposition === 'Warning').length,
    blocked: items.filter((item) => item.disposition === 'Blocked').length,
  };
}

@Injectable({ providedIn: 'root' })
export class TripImportApi {
  private readonly http = inject(HttpClient);

  preview(file: File): Promise<TripImport> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post<TripImport>('/api/trip-imports', form));
  }

  apply(id: string): Promise<TripImport> {
    return firstValueFrom(this.http.post<TripImport>(`/api/trip-imports/${id}/apply`, {}));
  }
}
