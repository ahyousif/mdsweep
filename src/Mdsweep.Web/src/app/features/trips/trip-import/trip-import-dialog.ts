import { Component, inject } from '@angular/core';

import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideFileSpreadsheet, lucideUpload } from '@ng-icons/lucide';
import { BrnDialogRef } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import {
  HlmDialogDescription,
  HlmDialogFooter,
  HlmDialogHeader,
  HlmDialogTitle,
} from '@spartan-ng/helm/dialog';

@Component({
  selector: 'app-trip-import-dialog',
  imports: [
    NgIcon,
    HlmButton,
    HlmDialogHeader,
    HlmDialogTitle,
    HlmDialogDescription,
    HlmDialogFooter,
  ],
  providers: [
    provideIcons({
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

  cancel(): void {
    this.#dialogRef.close();
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (!file) {
      return;
    }

    console.log('Selected file:', file);
  }
}
