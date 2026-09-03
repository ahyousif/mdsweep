import { DOCUMENT } from '@angular/common';
import { effect, inject, Injectable, signal } from '@angular/core';

export type ThemePreference = 'system' | 'light' | 'dark';

const STORAGE_KEY = 'mdsweep.theme';
const LEGACY_STORAGE_KEY = 'mdsweep.appearance';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly document = inject(DOCUMENT);
  private readonly mediaQuery = this.createMediaQuery();

  readonly preference = signal<ThemePreference>(this.readPreference());

  constructor() {
    effect((onCleanup) => {
      const preference = this.preference();

      this.apply(preference);

      if (preference !== 'system' || !this.mediaQuery) {
        return;
      }

      const handleChange = () => this.apply('system');

      this.mediaQuery.addEventListener('change', handleChange);

      onCleanup(() => {
        this.mediaQuery?.removeEventListener('change', handleChange);
      });
    });
  }

  setTheme(theme: ThemePreference): void {
    this.preference.set(theme);
    this.document.defaultView?.localStorage.setItem(STORAGE_KEY, theme);
  }

  private apply(preference: ThemePreference): void {
    const dark =
      preference === 'dark' || (preference === 'system' && this.mediaQuery?.matches === true);

    this.document.documentElement.classList.toggle('dark', dark);
  }

  private readPreference(): ThemePreference {
    const storage = this.document.defaultView?.localStorage;
    const preference = this.parsePreference(storage?.getItem(STORAGE_KEY));

    if (preference) {
      return preference;
    }

    const legacyPreference = this.parsePreference(storage?.getItem(LEGACY_STORAGE_KEY));

    if (legacyPreference) {
      storage?.setItem(STORAGE_KEY, legacyPreference);
      storage?.removeItem(LEGACY_STORAGE_KEY);
      return legacyPreference;
    }

    return 'system';
  }

  private parsePreference(value: string | null | undefined): ThemePreference | null {
    return value === 'light' || value === 'dark' || value === 'system' ? value : null;
  }

  private createMediaQuery(): MediaQueryList | null {
    const matchMedia = this.document.defaultView?.matchMedia;

    return typeof matchMedia === 'function'
      ? matchMedia.call(this.document.defaultView, '(prefers-color-scheme: dark)')
      : null;
  }
}
