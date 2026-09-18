import { TranslatePipe } from '@ngx-translate/core';
import { Component, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { HlmButton } from '@spartan-ng/helm/button';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmTextarea } from '@spartan-ng/helm/textarea';

import { type CreatePassengerDetails, type PassengerDetails } from './passengers.api';

@Component({
  selector: 'app-passenger-form',
  imports: [TranslatePipe, ReactiveFormsModule, HlmButton, HlmFieldImports, HlmInput, HlmSpinner, HlmTextarea],
  templateUrl: './passenger-form.html',
})
export default class PassengerForm {
  readonly busy = input(false);
  readonly value = input<PassengerDetails | null>(null);
  readonly editing = input(false);
  readonly saved = output<PassengerDetails>();
  readonly cancelled = output<void>();
  readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]],
    lastName: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]],
    brokerMemberId: ['', Validators.maxLength(200)],
    dateOfBirth: [''],
    phoneNumber: ['', Validators.maxLength(50)],
    alternatePhoneNumber: ['', Validators.maxLength(50)],
    passengerType: ['', Validators.maxLength(200)],
    specialNeeds: ['', Validators.maxLength(500)],
    notes: ['', Validators.maxLength(2000)],
  });

  constructor() {
    effect(() => {
      const value = this.value();
      this.form.reset({
        firstName: value?.firstName ?? '',
        lastName: value?.lastName ?? '',
        brokerMemberId: value?.brokerMemberId ?? '',
        dateOfBirth: value?.dateOfBirth ?? '',
        phoneNumber: value?.phoneNumber ?? '',
        alternatePhoneNumber: value?.alternatePhoneNumber ?? '',
        passengerType: value?.passengerType ?? '',
        specialNeeds: value?.specialNeeds ?? '',
        notes: value?.notes ?? '',
      });
    });
  }

  submit(): void {
    this.form.markAllAsTouched();
    if (this.busy() || !this.form.valid) return;

    const value = this.form.getRawValue();
    this.saved.emit({
      firstName: value.firstName.trim(),
      lastName: value.lastName.trim(),
      brokerMemberId: value.brokerMemberId.trim() || null,
      dateOfBirth: value.dateOfBirth || null,
      phoneNumber: value.phoneNumber.trim() || null,
      alternatePhoneNumber: value.alternatePhoneNumber.trim() || null,
      passengerType: value.passengerType.trim() || null,
      specialNeeds: value.specialNeeds.trim() || null,
      notes: value.notes.trim() || null,
    });
  }
}
