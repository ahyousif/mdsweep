import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { toApplicationError } from '../errors/application-error';
import { LanguageService } from '../i18n/language.service';
import { provideLocalization } from '../i18n/localization.providers';
import { UiMessagePipe } from '../i18n/ui-message';
import { httpErrorMessage } from './http-error-message';

describe('API feedback', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideLocalization(), UiMessagePipe] });
    TestBed.inject(LanguageService).setLanguage('en');
  });
  afterEach(() => {
    localStorage.removeItem('mdsweep.language');
    document.documentElement.lang = 'en';
    document.documentElement.dir = 'ltr';
  });

  it('preserves API issues and retranslates multiple business failures without another request', () => {
    const issues = [
      { field: 'email', code: 'membershipExists' },
      { field: 'user', code: 'protectOwnAccess' },
    ];
    const error = toApplicationError(
      new HttpErrorResponse({
        status: 400,
        error: {
          errors: { email: ['This user already belongs to this Tenant.'] },
          issues,
        },
      }),
    );
    expect(error.issues).toEqual(issues);
    const message = httpErrorMessage(error, 'errors.saveUser');
    const pipe = TestBed.inject(UiMessagePipe);
    expect(pipe.transform(message)).toContain('already belongs');
    expect(pipe.transform(message)).toContain('cannot deactivate yourself');
    TestBed.inject(LanguageService).setLanguage('ar');
    expect(pipe.transform(message)).toContain('لا يمكنك تعطيل حسابك');
    expect(error.message).toBe('This user already belongs to this Tenant.');
  });

  it('keeps ordinary validation diagnostics and supplies localized recovery without business codes', () => {
    const errors = { Email: ['Email must be valid.'] };
    const error = toApplicationError(new HttpErrorResponse({ status: 400, error: { errors } }));
    expect(error.validationErrors).toEqual(errors);
    expect(error.issues).toEqual([]);
    const message = httpErrorMessage(error, 'errors.saveUser');
    const pipe = TestBed.inject(UiMessagePipe);
    expect(pipe.transform(message)).toBe('Check the entered values and try again.');
    TestBed.inject(LanguageService).setLanguage('ar');
    expect(pipe.transform(message)).toBe('تحقق من القيم المدخلة وحاول مجددًا.');
  });

  it.each([
    [0, 'errors.network'],
    [401, 'errors.unauthorized'],
    [403, 'errors.forbidden'],
    [404, 'errors.notFound'],
    [409, 'errors.conflict'],
    [400, 'errors.saveUser'],
    [500, 'errors.saveUser'],
  ])('maps HTTP %s to recovery %s', (status, key) => {
    expect(
      httpErrorMessage(
        toApplicationError(new HttpErrorResponse({ status: Number(status) })),
        'errors.saveUser',
      ),
    ).toEqual({ key });
  });

  it('uses generic feedback for unknown issues and caller recovery for non-HTTP errors', () => {
    const error = toApplicationError(
      new HttpErrorResponse({
        status: 400,
        error: {
          issues: [{ code: 'futureBusinessCondition' }],
        },
      }),
    );
    expect(
      TestBed.inject(UiMessagePipe).transform(httpErrorMessage(error, 'errors.saveUser')),
    ).toBe('The request could not be completed. Try again.');
    expect(
      httpErrorMessage(toApplicationError(new Error('Unexpected')), 'errors.saveUser'),
    ).toEqual({ key: 'errors.saveUser' });
  });
});
