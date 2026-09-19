import { Component, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { type VehicleDetails } from './vehicles.api';

@Component({
  selector: 'app-vehicle-form',
  imports: [
    ReactiveFormsModule,
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
  readonly busy = input(false);
  readonly saved = output<VehicleDetails>();
  readonly cancelled = output();
  readonly form = inject(NonNullableFormBuilder).group({
    displayLabel: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(100)]],
    vin: ['', [Validators.required, Validators.pattern(/^[A-HJ-NPR-Za-hj-npr-z0-9]{17}$/)]],
  });
  constructor() {
    effect(() => this.form.reset(this.value() ?? { displayLabel: '', vin: '' }));
  }
  submit(): void {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const details = this.form.getRawValue();
    this.saved.emit(details);
  }
}
