import { Component, computed, effect, inject, input, output } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmInput } from '@spartan-ng/helm/input';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { HlmCheckbox } from '@spartan-ng/helm/checkbox';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { type UserDetails, type UserRole } from './users.api';

@Component({
  selector: 'app-user-form',
  imports: [ReactiveFormsModule, HlmButton, HlmInput, HlmCheckbox, HlmFieldImports, HlmSpinner],
  templateUrl: './user-form.html',
})
export class UserForm {
  readonly value = input<UserDetails | null>(null);
  readonly inviting = input(false);
  readonly administrator = input(false);
  readonly busy = input(false);
  readonly availableRoles = computed<UserRole[]>(() =>
    this.administrator() ? ['Driver', 'Dispatcher', 'Administrator'] : ['Driver'],
  );
  readonly prefix = input('user');
  readonly saved = output<UserDetails>();
  readonly cancelled = output();
  readonly form = inject(NonNullableFormBuilder).group({
    firstName: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]],
    lastName: ['', [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]],
    email: [''],
    roles: inject(NonNullableFormBuilder).control<UserRole[]>(
      ['Driver'],
      [
        (control) =>
          control.value.length >= 1 && control.value.length <= 2 ? null : { roles: true },
      ],
    ),
    isActive: [true],
  });
  constructor() {
    effect(() => {
      this.form.reset(
        this.value() ?? {
          firstName: '',
          lastName: '',
          email: '',
          roles: ['Driver'],
          isActive: true,
        },
      );
      this.form.controls.email.setValidators(
        this.inviting() ? [Validators.required, Validators.email] : [],
      );
      this.form.controls.email.updateValueAndValidity();
    });
  }
  setRole(role: UserRole, checked: boolean): void {
    const control = this.form.controls.roles;
    const roles = control.value;
    if (!checked) control.setValue(roles.filter((value) => value !== role));
    else if (!roles.includes(role) && roles.length < 2) control.setValue([...roles, role]);
    control.markAsTouched();
  }
  submit(): void {
    if (this.busy()) return;
    this.form.markAllAsTouched();
    if (this.form.valid) this.saved.emit(this.form.getRawValue());
  }
}
