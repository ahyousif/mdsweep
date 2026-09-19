import { Component, effect, input, output, signal, untracked } from '@angular/core';
import {
  form, FormField, max, maxLength, min, pattern, required, submit, validate,
} from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { type VehicleDetails } from './vehicles.api';

@Component({
  selector: 'app-vehicle-form',
  imports: [
    FormField,
    TranslatePipe,
    HlmButton,
    HlmInput,
    HlmSpinner,
    ...HlmFieldImports,
  ],
  templateUrl: './vehicle-form.html',
})
export class VehicleForm {
  readonly value = input<VehicleDetails | null>(null);
  readonly mutationPending = input(false);
  readonly saved = output<VehicleDetails>();
  readonly cancelled = output();
  readonly maxYear = new Date().getUTCFullYear() + 1;
  private readonly model = signal({
    displayLabel: '',
    vin: '',
    year: null as number | null,
    make: '',
    model: '',
  });
  readonly form = form(this.model, (fields) => {
    required(fields.displayLabel);
    pattern(fields.displayLabel, /\S/);
    maxLength(fields.displayLabel, 100);
    required(fields.vin);
    maxLength(fields.vin, 17);
    pattern(fields.vin, /^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$/);
    min(fields.year, 1900);
    max(fields.year, this.maxYear);
    validate(fields.year, ({ value }) =>
      value() === null || Number.isInteger(value()) ? null : { kind: 'integer' },
    );
    maxLength(fields.make, 100);
    maxLength(fields.model, 100);
  });

  constructor() {
    effect(() => {
      const value = this.value();
      untracked(() => {
        this.form().reset({
          displayLabel: value?.displayLabel ?? '',
          vin: value?.vin ?? '',
          year: value?.year ?? null,
          make: value?.make ?? '',
          model: value?.model ?? '',
        });
      });
    });
  }

  async onSubmit(event: Event): Promise<void> {
    event.preventDefault();
    if (this.mutationPending()) return;
    await submit(this.form, async () => {
      const details = this.model();
      this.saved.emit({ ...details, make: details.make || null, model: details.model || null });
    });
  }
}
