import { Component, effect, inject, input, output } from '@angular/core';
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
  readonly busy = input(false);
  readonly availableRoles: UserRole[] = ['Driver', 'Dispatcher', 'Administrator'];
  readonly prefix = input('user');
  readonly saved = output<UserDetails>();
  readonly cancelled = output();
  readonly form = inject(NonNullableFormBuilder).group({
    firstName: [''],
    lastName: [''],
    displayName: [''],
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
          displayName: '',
          firstName: '',
          lastName: '',
          email: '',
          roles: ['Driver'],
          isActive: true,
        },
      );
      for (const field of ['firstName', 'lastName'] as const) {
        this.form.controls[field].setValidators(
          this.inviting()
            ? [Validators.required, Validators.pattern(/\S/), Validators.maxLength(200)]
            : [],
        );
        this.form.controls[field].updateValueAndValidity();
      }
      this.form.controls.displayName.setValidators(
        this.inviting()
          ? []
          : [Validators.required, Validators.pattern(/\S/), Validators.maxLength(401)],
      );
      this.form.controls.displayName.updateValueAndValidity();
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
