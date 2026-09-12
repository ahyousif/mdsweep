import { DOCUMENT } from '@angular/common';
import { Directionality } from '@angular/cdk/bidi';
import { computed, effect, inject, Service, untracked } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

export type Language = 'en' | 'ar';
const storageKey = 'mdsweep.language';

@Service()
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly document = inject(DOCUMENT);
  private readonly bidi = inject(Directionality);
  readonly language = computed<Language>(() =>
    this.translate.currentLang() === 'ar' ? 'ar' : 'en',
  );
  readonly direction = computed(() => (this.language() === 'ar' ? 'rtl' : 'ltr'));
  readonly locale = computed(() =>
    this.language() === 'ar' ? 'ar-u-ca-gregory-nu-latn' : 'en-US',
  );

  constructor() {
    effect(() => {
      const language = this.language();
      const direction = this.direction();
      untracked(() => {
        this.document.documentElement.lang = language;
        this.document.documentElement.dir = direction;
        if (this.bidi.value !== direction) {
          this.bidi.valueSignal.set(direction);
          this.bidi.change.emit(direction);
        }
      });
    });
  }

  initialize(): void {
    let language: Language = 'en';
    try {
      if (this.document.defaultView?.localStorage.getItem(storageKey) === 'ar') language = 'ar';
    } catch {
      /* Storage may be unavailable; the preference still works for this session. */
    }
    this.setLanguage(language);
  }

  setLanguage(language: Language): void {
    if (language !== 'en' && language !== 'ar') return;
    // Catalogs are registered at bootstrap, so activation is synchronous and cannot fail offline.
    this.translate.use(language);
    try {
      this.document.defaultView?.localStorage.setItem(storageKey, language);
    } catch {
      /* A blocked preference store must not prevent switching. */
    }
  }

  formatDate(
    value: Date | string,
    options: Intl.DateTimeFormatOptions = { year: 'numeric', month: 'short', day: 'numeric' },
  ): string {
    return new Intl.DateTimeFormat(this.locale(), options).format(
      typeof value === 'string' ? new Date(value) : value,
    );
  }

  formatTime(value: string | null, fallbackKey = 'common.notSupplied'): string {
    if (!value) return this.translate.instant(fallbackKey);
    const [hours, minutes] = value.split(':').map(Number);
    return this.formatDate(new Date(2000, 0, 1, hours, minutes), {
      hour: 'numeric',
      minute: '2-digit',
      hour12: true,
    });
  }
}
