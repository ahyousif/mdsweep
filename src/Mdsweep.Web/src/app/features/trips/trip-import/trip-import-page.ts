import { Component, inject, signal } from '@angular/core';
import { HlmAlertImports } from '@spartan-ng/helm/alert';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmCardImports } from '@spartan-ng/helm/card';
import { injectMutation } from '@tanstack/angular-query-experimental';
import { httpErrorMessage } from '../../../core/api/http-error-message';
import { ManifestImportApi, ManifestPreview } from './trip-import.api';

@Component({
  selector: 'app-trip-import-page',
  imports: [HlmButton, ...HlmAlertImports, ...HlmCardImports],
  templateUrl: './trip-import-page.html',
})
export class TripImportPage {
  private readonly api = inject(ManifestImportApi);
  protected readonly preview = signal<ManifestPreview | null>(null);
  protected readonly error = signal('');
  protected readonly previewMutation = injectMutation(() => ({
    mutationFn: (file: File) => this.api.preview(file),
    onSuccess: (preview) => this.preview.set(preview),
    onError: (error) => this.error.set(httpErrorMessage(error, 'Unable to check this Trip Import.')),
  }));
  protected readonly applyMutation = injectMutation(() => ({
    mutationFn: (previewId: string) => this.api.apply(previewId),
    onError: (error) => this.error.set(httpErrorMessage(error, 'Unable to import trips.')),
  }));

  protected chooseFile(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (file) this.previewMutation.mutate(file);
  }

  protected apply(): void {
    const preview = this.preview();
    if (preview) this.applyMutation.mutate(preview.previewId);
  }
}
