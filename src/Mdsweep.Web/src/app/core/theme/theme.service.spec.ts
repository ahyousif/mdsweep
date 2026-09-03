import { DOCUMENT } from '@angular/common';
import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let storage: Storage;

  beforeEach(() => {
    storage = window.localStorage;
    storage.clear();
  });

  afterEach(() => storage.clear());

  it('migrates a valid appearance preference to the theme preference key', () => {
    storage.setItem('mdsweep.appearance', 'dark');
    TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: document }] });

    const service = TestBed.inject(ThemeService);

    expect(service.preference()).toBe('dark');
    expect(storage.getItem('mdsweep.theme')).toBe('dark');
    expect(storage.getItem('mdsweep.appearance')).toBeNull();
  });

  it('persists an explicit theme preference', () => {
    TestBed.configureTestingModule({ providers: [{ provide: DOCUMENT, useValue: document }] });
    const service = TestBed.inject(ThemeService);

    service.setTheme('light');

    expect(service.preference()).toBe('light');
    expect(storage.getItem('mdsweep.theme')).toBe('light');
  });
});
