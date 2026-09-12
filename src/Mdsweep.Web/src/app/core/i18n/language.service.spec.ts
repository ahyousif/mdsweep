import { DOCUMENT } from '@angular/common';
import { Directionality } from '@angular/cdk/bidi';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { LanguageService } from './language.service';
import { provideLocalization } from './localization.providers';
import { UiMessagePipe } from './ui-message';

describe('Language preference', () => {
  beforeEach(() => {
    localStorage.removeItem('mdsweep.language');
    TestBed.configureTestingModule({ providers: [provideLocalization(), UiMessagePipe] });
  });
  afterEach(() => {
    localStorage.removeItem('mdsweep.language');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
  });

  it('defaults to English even for an unsupported saved preference', async () => {
    localStorage.setItem('mdsweep.language', 'fr');
    const language = TestBed.inject(LanguageService);
    await language.initialize();
    TestBed.tick();
    expect(language.language()).toBe('en');
    expect(TestBed.inject(TranslateService).instant('trips.title')).toBe('Trips');
    expect(TestBed.inject(DOCUMENT).documentElement.dir).toBe('ltr');
  });

  it('restores Arabic and synchronizes document and overlay direction', async () => {
    localStorage.setItem('mdsweep.language', 'ar');
    const language = TestBed.inject(LanguageService);
    await language.initialize();
    TestBed.tick();
    expect(document.documentElement.lang).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
    expect(TestBed.inject(Directionality).value).toBe('rtl');
    expect(language.formatDate(new Date(2026, 8, 12))).toContain('2026');
    expect(language.formatDate(new Date(2026, 8, 12))).not.toMatch(/[٠-٩]/);
    expect(language.formatTime('09:30:00')).toContain('9:30');
  });

  it('keeps the latest selection when switching repeatedly', async () => {
    const language = TestBed.inject(LanguageService);
    await Promise.all([
      language.setLanguage('ar'),
      language.setLanguage('en'),
      language.setLanguage('ar'),
    ]);
    TestBed.tick();
    expect(language.language()).toBe('ar');
    expect(localStorage.getItem('mdsweep.language')).toBe('ar');
    expect(document.documentElement.dir).toBe('rtl');
  });

  it('renders Arabic plural forms with western digits and updates retained feedback', async () => {
    const language = TestBed.inject(LanguageService);
    const translate = TestBed.inject(TranslateService);
    const pipe = TestBed.inject(UiMessagePipe);
    const feedback = { key: 'users.invitationSent', params: { email: 'synthetic@example.test' } };
    expect(pipe.transform(feedback)).toContain('Invitation sent to');
    await language.setLanguage('ar');
    expect(pipe.transform(feedback)).toContain('تم إرسال الدعوة');
    expect(
      [0, 1, 2, 3, 11, 100].map((count) => translate.instant('trips.count', { count })),
    ).toEqual(['لا توجد رحلات', 'رحلة واحدة', 'رحلتان', '3 رحلات', '11 رحلة', '100 رحلة']);
    expect(pipe.transform({ key: 'errors.unknownFutureCode' })).toContain('تعذر إكمال الطلب');
  });
});
