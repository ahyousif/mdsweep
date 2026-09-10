import { Component, computed, inject, signal } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCheck,
  lucideCircleAlert,
  lucideFileSpreadsheet,
  lucideUpload,
} from '@ng-icons/lucide';
import { BrnDialogRef } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmDialogDescription,
  HlmDialogFooter,
  HlmDialogHeader,
  HlmDialogTitle,
} from '@spartan-ng/helm/dialog';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectMutation } from '@tanstack/angular-query-experimental';
import { QueryClient } from '@tanstack/query-core';

import { httpErrorMessage } from '@app/core/api/http-error-message';

import { tripQueryKeys } from '../trips.queries';
import { TripImportApi, TripImportResult } from './trip-import.api';

type ImportStep = 'upload' | 'complete';

@Component({
  selector: 'app-trip-import-dialog',
  imports: [
    NgIcon,
    HlmButton,
    HlmSpinner,
    HlmDialogHeader,
    HlmDialogTitle,
    HlmDialogDescription,
    HlmDialogFooter,
  ],
  providers: [
    provideIcons({
      lucideCheck,
      lucideCircleAlert,
      lucideFileSpreadsheet,
      lucideUpload,
    }),
  ],
  host: {
    class: 'flex flex-col gap-6',
  },
  templateUrl: './trip-import-dialog.html',
})
export default class TripImportDialog {
  readonly #dialogRef = inject(BrnDialogRef);
  readonly #api = inject(TripImportApi);
  readonly #queryClient = inject(QueryClient);

  readonly step = signal<ImportStep>('upload');
  readonly file = signal<File | null>(null);
  readonly result = signal<TripImportResult | null>(null);
  readonly dragging = signal(false);
  readonly fileError = signal('');

  readonly importMutation = injectMutation(() => ({
    mutationFn: (file: File) => this.#api.import(file),

    onSuccess: async (result) => {
      this.result.set(result);

      await this.#queryClient.invalidateQueries({
        queryKey: tripQueryKeys.all,
      });

      this.step.set('complete');
    },
  }));

  readonly importing = this.importMutation.isPending;

  readonly importError = computed(() => {
    const error = this.importMutation.error();

    return error
      ? httpErrorMessage(error, 'Trips could not be imported. Check the file and try again.')
      : '';
  });

  readonly error = computed(() => this.fileError() || this.importError());

  readonly fileType = computed(() => {
    const file = this.file();

    if (!file) {
      return '';
    }

    return file.name.toLowerCase().endsWith('.csv') ? 'CSV file' : 'Excel file';
  });

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (file) {
      this.#selectFile(file);
    }

    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();

    if (!this.importing()) {
      this.dragging.set(true);
    }
  }

  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);

    if (this.importing()) {
      return;
    }

    const file = event.dataTransfer?.files[0];

    if (file) {
      this.#selectFile(file);
    }
  }

  changeFile(): void {
    if (this.importing()) {
      return;
    }

    this.file.set(null);
    this.fileError.set('');
    this.importMutation.reset();
  }

  importTrips(): void {
    const file = this.file();

    if (!file || this.importing()) {
      return;
    }

    this.importMutation.mutate(file);
  }

  cancel(): void {
    if (this.importing()) {
      return;
    }

    this.#dialogRef.close();
  }

  done(): void {
    this.#dialogRef.close();
  }

  #selectFile(file: File): void {
    if (!isSupportedFile(file)) {
      this.fileError.set('Choose a CSV or Excel (.xlsx) file.');
      return;
    }

    this.fileError.set('');
    this.importMutation.reset();
    this.file.set(file);
  }
}

function isSupportedFile(file: File): boolean {
  const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();

  return extension === '.csv' || extension === '.xlsx';
}
