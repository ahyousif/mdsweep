import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

export type PreviewRow = {
  tripNumber: string;
  disposition: 'Ready' | 'Warning' | 'Blocked';
  brokerChange: 'New' | 'BrokerChanged' | 'Unchanged' | 'Blocked';
  hasProviderOverrides: boolean;
  isActive: boolean;
  messages: string[];
};

export type ManifestPreview = {
  previewId: string;
  ready: number;
  warning: number;
  blocked: number;
  serviceDates: string[];
  rows: PreviewRow[];
};

@Injectable({ providedIn: 'root' })
export class ManifestImportApi {
  private readonly http = inject(HttpClient);

  preview(file: File): Promise<ManifestPreview> {
    const form = new FormData();
    form.append('file', file);
    return firstValueFrom(this.http.post<ManifestPreview>('/api/manifest-imports/preview', form));
  }

  apply(previewId: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(`/api/manifest-imports/${previewId}/apply`, {}));
  }
}
