import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from './language.service';

@Component({
  selector: 'app-language-picker',
  imports: [TranslatePipe],
  template: `
    <label class="flex items-center gap-2 text-sm">
      <span>{{ 'common.language' | translate }}</span>
      <select
        class="bg-background text-foreground border-input min-h-10 rounded-md border px-2 py-1"
        [value]="language.language()"
        (change)="select($event)"
      >
        <option value="en" lang="en" dir="ltr">English</option>
        <option value="ar" lang="ar" dir="rtl">العربية</option>
      </select>
    </label>
  `,
})
export class LanguagePicker {
  readonly language = inject(LanguageService);
  select(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === 'en' || value === 'ar') void this.language.setLanguage(value);
  }
}
