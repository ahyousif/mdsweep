import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TripImportApi } from './trip-import.api';

describe('TripImportApi', () => {
  let api: TripImportApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(TripImportApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts a multipart file directly to Import Trips', async () => {
    const importing = api.import(new File(['trip'], 'synthetic.csv', { type: 'text/csv' }));
    const post = http.expectOne('/api/trips/import');
    expect(post.request.method).toBe('POST');
    expect(post.request.body.get('file').name).toBe('synthetic.csv');
    post.flush({ fileName: 'synthetic.csv', total: 1, added: 1, updated: 0, unchanged: 0, problemCount: 0, problems: [] });
    await expect(importing).resolves.toMatchObject({ added: 1 });
  });
});
