import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ApiClient, API_BASE_PATH } from './api-client';

describe('ApiClient', () => {
  it('builds feature endpoints from the configured API base path', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        { provide: API_BASE_PATH, useValue: '/gateway/api' },
      ],
    });

    expect(TestBed.inject(ApiClient).url('/trips')).toBe('/gateway/api/trips');
  });
});
