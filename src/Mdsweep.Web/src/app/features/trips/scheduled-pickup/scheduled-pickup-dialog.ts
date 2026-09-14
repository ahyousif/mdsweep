import { Component, computed, inject, signal } from '@angular/core';
import { FormField, disabled, form, validate, required } from '@angular/forms/signals';
import { NgIcon, provideIcons } from '@ng-icons/core';
import {
  lucideCalendarDays,
  lucideCarFront,
  lucideClock3,
  lucideRotateCcw,
  lucideX,
} from '@ng-icons/lucide';
import { HlmBadge } from '@spartan-ng/helm/badge';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { BrnDialogRef, injectBrnDialogContext } from '@spartan-ng/brain/dialog';
import { HlmButton } from '@spartan-ng/helm/button';
import { HlmDialogImports } from '@spartan-ng/helm/dialog';
import { HlmFieldImports } from '@spartan-ng/helm/field';
import { HlmInputGroupImports } from '@spartan-ng/helm/input-group';
import { HlmSpinner } from '@spartan-ng/helm/spinner';
import { injectMutation, QueryClient } from '@tanstack/angular-query-experimental';
import { httpErrorMessage } from '@app/core/api/http-error-message';
import { LanguageService } from '@app/core/i18n/language.service';
import { UiMessagePipe } from '@app/core/i18n/ui-message';
import { Trip } from '../trips-types';
import { TripsApi } from '../trips.api';
import { scheduledPickupMutationOptions } from '../trips.queries';
import { parseScheduledPickupTime } from './scheduled-pickup-time';

@Component({
  selector: 'app-scheduled-pickup-dialog',
  imports: [
    NgIcon,
    HlmBadge,
    FormField,
    TranslatePipe,
    UiMessagePipe,
    HlmButton,
    HlmInputGroupImports,
    HlmSpinner,
    HlmDialogImports,
    HlmFieldImports,
  ],
  providers: [
    provideIcons({ lucideCalendarDays, lucideCarFront, lucideClock3, lucideRotateCcw, lucideX }),
  ],
  host: { class: 'flex min-w-0 flex-col gap-4' },
  templateUrl: './scheduled-pickup-dialog.html',
})
export default class ScheduledPickupDialog {
  readonly trip = injectBrnDialogContext<{ trip: Trip }>().trip;
  readonly language = inject(LanguageService);
  readonly #translate = inject(TranslateService);
  readonly #dialogRef = inject(BrnDialogRef);
  readonly #api = inject(TripsApi);
  readonly #queryClient = inject(QueryClient);
  readonly model = signal({
    time: this.trip.scheduledPickupTime
      ? this.language.formatTime(this.trip.scheduledPickupTime)
      : '',
  });
  readonly mutation = injectMutation(() => {
    const options = scheduledPickupMutationOptions(this.#api, this.#queryClient);
    return {
      ...options,
      onSuccess: async () => {
        await options.onSuccess();
        this.#dialogRef.close();
      },
    };
  });
  readonly saving = this.mutation.isPending;
  readonly timeForm = form(this.model, (path) => {
    required(path.time);
    validate(path.time, ({ value }) =>
      value() && parseScheduledPickupTime(value()) === null ? { kind: 'time' } : undefined,
    );
    disabled(path.time, () => this.saving());
  });
  readonly error = computed(() =>
    this.mutation.error()
      ? httpErrorMessage(this.mutation.error(), 'errors.saveScheduledPickup')
      : null,
  );
  readonly driveEstimate = computed(() => {
    this.language.language();
    const minutes = this.trip.estimatedTravelMinutes;
    const meters = this.trip.estimatedDistanceMeters;
    if (minutes === null) return this.#translate.instant('common.notAvailable');
    return this.#translate.instant(meters === null ? 'trips.driveMinutes' : 'trips.driveDistance', {
      minutes,
      miles: meters === null ? '' : (meters / 1609.344).toFixed(1),
    });
  });

  normalizeTime(): void {
    if (this.saving()) return;
    const time = parseScheduledPickupTime(this.model().time);
    if (time !== null) this.model.set({ time: this.language.formatTime(time) });
  }

  save(): void {
    if (this.saving()) return;
    const time = parseScheduledPickupTime(this.model().time);
    if (time === null) {
      this.timeForm.time().markAsTouched();
      return;
    }
    this.model.set({ time: this.language.formatTime(time) });
    this.mutation.mutate({ id: this.trip.id, time });
  }

  reset(): void {
    if (this.saving()) return;
    this.mutation.mutate({ id: this.trip.id, time: null });
  }

  cancel(): void {
    if (!this.saving()) this.#dialogRef.close();
  }
}
