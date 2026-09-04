import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { injectMutation } from '@tanstack/angular-query-experimental';
import { QueryClient } from '@tanstack/query-core';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { tripQueryKeys } from '../all-trips/all-trips.queries';
import {
  TripImportApi,
  TripImportResult,
} from './trip-import.api';

@Component({
  selector: 'app-trip-import-page',
  imports: [RouterLink, HlmButton, ...HlmAlertImports, ...HlmCardImports],
  templateUrl: './trip-import-page.html',
})
export default class TripImportPage {
  readonly #api = inject(TripImportApi);
  readonly #queryClient = inject(QueryClient);
  readonly selectedFile = signal<File | null>(null);
  readonly result = signal<TripImportResult | null>(null);
  readonly error = signal('');
  readonly importMutation = injectMutation(() => ({
    mutationFn: (file: File) => this.#api.import(file),
    onSuccess: (result) => {
      this.error.set('');
      this.result.set(result);
      this.#queryClient.invalidateQueries({ queryKey: tripQueryKeys.all });
    },
    onError: (error) => this.error.set(httpErrorMessage(error, 'Unable to import trips.')),
  }));

  chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) { this.selectedFile.set(file); this.result.set(null); this.error.set(''); }
  }

  importTrips(): void {
    const file = this.selectedFile();
    if (file) {
      this.error.set('');
      this.importMutation.mutate(file);
    }
  }
}
