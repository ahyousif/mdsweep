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

import { httpErrorMessage } from '@app/core/api/http-error-message';

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

  readonly step = signal<ImportStep>('upload');
  readonly file = signal<File | null>(null);
  readonly result = signal<TripImportResult | null>(null);

  readonly importing = signal(false);
  readonly dragging = signal(false);
  readonly error = signal('');

  readonly fileType = computed(() => {
    const file = this.file();

    if (!file) {
      return '';
    }

    return file.name.toLowerCase().endsWith('.csv') ? 'CSV file' : 'Excel file';
  });

  readonly totalProcessed = computed(() => {
    const result = this.result();

    if (!result) {
      return 0;
    }

    return result.readyCount + result.needsAttentionCount;
  });

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (file) {
      this.selectFile(file);
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
      this.selectFile(file);
    }
  }

  changeFile(): void {
    if (this.importing()) {
      return;
    }

    this.file.set(null);
    this.error.set('');
  }

  async importTrips(): Promise<void> {
    const file = this.file();

    if (!file || this.importing()) {
      return;
    }

    this.importing.set(true);
    this.error.set('');

    try {
      const result = await this.#api.import(file);

      this.result.set(result);
      this.step.set('complete');
    } catch (error) {
      this.error.set(
        httpErrorMessage(error, 'Trips could not be imported. Check the file and try again.'),
      );
    } finally {
      this.importing.set(false);
    }
  }

  cancel(): void {
    if (this.importing()) {
      return;
    }

    this.#dialogRef.close();
  }

  done(): void {
    this.#dialogRef.close('imported');
  }

  private selectFile(file: File): void {
    if (!isSupportedFile(file)) {
      this.error.set('Choose a CSV or Excel (.xlsx) file.');

      return;
    }

    this.file.set(file);
    this.error.set('');
  }
}

function isSupportedFile(file: File): boolean {
  const extension = file.name.slice(file.name.lastIndexOf('.')).toLowerCase();

  return extension === '.csv' || extension === '.xlsx';
}
