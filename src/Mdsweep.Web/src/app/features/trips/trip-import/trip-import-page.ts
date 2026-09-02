import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { injectMutation, injectQueryClient } from '@tanstack/angular-query-experimental';
import { httpErrorMessage } from '../../../core/api/http-error-message';
import { tripQueryKeys } from '../all-trips/all-trips.queries';
import {
  TripImport,
  TripImportApi,
  TripImportItem,
  tripImportDispositionCounts,
} from './trip-import.api';

@Component({
  selector: 'app-trip-import-page',
  imports: [RouterLink, HlmButton, ...HlmAlertImports, ...HlmCardImports],
  templateUrl: './trip-import-page.html',
})
export class TripImportPage {
  private readonly api = inject(TripImportApi);
  private readonly queryClient = injectQueryClient();
  protected readonly preview = signal<TripImport | null>(null);
  protected readonly error = signal('');
  protected readonly previewMutation = injectMutation(() => ({
    mutationFn: (file: File) => this.api.preview(file),
    onSuccess: (preview) => this.preview.set(preview),
    onError: (error) => this.error.set(httpErrorMessage(error, 'Unable to check this Trip Import.')),
  }));
  protected readonly applyMutation = injectMutation(() => ({
    mutationFn: (id: string) => this.api.apply(id),
    onSuccess: (tripImport) => {
      this.preview.set(tripImport);
      this.queryClient.invalidateQueries({ queryKey: tripQueryKeys.all });
    },
    onError: (error) => this.error.set(httpErrorMessage(error, 'Unable to import trips.')),
  }));

  protected chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.previewMutation.mutate(file);
  }

  protected apply(): void {
    const preview = this.preview();
    if (preview) this.applyMutation.mutate(preview.id);
  }

  protected count(disposition: string): number {
    const counts = tripImportDispositionCounts(this.preview()?.items ?? []);
    return counts[disposition.toLowerCase() as keyof typeof counts] ?? 0;
  }

  protected messages(item: TripImportItem): string {
    return item.messages.join(' ');
  }
}
