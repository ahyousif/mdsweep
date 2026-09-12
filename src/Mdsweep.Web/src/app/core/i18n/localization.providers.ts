import { inject, provideAppInitializer, provideEnvironmentInitializer } from '@angular/core';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';
import english from './generated/en.js';
import arabic from './generated/ar.js';
import { LanguageService } from './language.service';

export function provideLocalization() {
  return [
    provideTranslateService(),
    provideEnvironmentInitializer(() => {
      const translate = inject(TranslateService);
      // Two small, precompiled catalogs make switching immediate, including offline.
      translate.setCompiledTranslation('en', english);
      translate.setCompiledTranslation('ar', arabic);
      translate.setFallbackLang('en');
      translate.use('en');
    }),
    provideAppInitializer(() => inject(LanguageService).initialize()),
  ];
}
