import { Directive, effect, inject, untracked } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { injectBrnCalendarI18n } from '@spartan-ng/brain/calendar';
import { LanguageService } from './language.service';

// Keep the calendar dependency in the lazy feature that actually renders it.
@Directive({ selector: '[appLocalizedCalendar]' })
export class LocalizedCalendar {
  private readonly language = inject(LanguageService);
  private readonly translate = inject(TranslateService);
  private readonly calendar = injectBrnCalendarI18n();
  constructor() {
    effect(() => {
      const locale = this.language.locale();
      untracked(() => {
        const month = new Intl.DateTimeFormat(locale, { month: 'long' });
        const weekday = new Intl.DateTimeFormat(locale, { weekday: 'short' });
        const fullWeekday = new Intl.DateTimeFormat(locale, { weekday: 'long' });
        const header = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' });
        this.calendar.use({
          formatMonth: (index) => month.format(new Date(2024, index, 1)),
          formatHeader: (index, year) => header.format(new Date(year, index, 1)),
          formatYear: (year) => String(year),
          formatWeekdayName: (index) => weekday.format(new Date(2024, 0, 7 + index)),
          labelWeekday: (index) => fullWeekday.format(new Date(2024, 0, 7 + index)),
          labelPrevious: () => this.translate.instant('common.previousMonth'),
          labelNext: () => this.translate.instant('common.nextMonth'),
          months: () =>
            Array.from({ length: 12 }, (_, index) => month.format(new Date(2024, index, 1))) as [
              string,
              string,
              string,
              string,
              string,
              string,
              string,
              string,
              string,
              string,
              string,
              string,
            ],
        });
      });
    });
  }
}
