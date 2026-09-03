import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { TripImportApi, tripImportDispositionCounts } from './trip-import.api';

describe('TripImportApi', () => {
  let api: TripImportApi;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(TripImportApi);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts a multipart file to the Trip Import endpoint and applies the returned id', async () => {
    const preview = api.preview(new File(['trip'], 'synthetic.csv', { type: 'text/csv' }));
    const post = http.expectOne('/api/trip-imports');
    expect(post.request.method).toBe('POST');
    expect(post.request.body.get('file').name).toBe('synthetic.csv');
    post.flush({ id: 'f61d5a5f-68f2-4b8f-9a76-4d3a1bd5d9eb', fileName: 'synthetic.csv', status: 'Previewed', appliedAt: null, items: [] });
    const result = await preview;

    const apply = api.apply(result.id);
    const applyPost = http.expectOne('/api/trip-imports/f61d5a5f-68f2-4b8f-9a76-4d3a1bd5d9eb/apply');
    expect(applyPost.request.method).toBe('POST');
    applyPost.flush({ ...result, status: 'Applied', appliedAt: '2026-09-02T12:00:00Z' });
    await expect(apply).resolves.toMatchObject({ status: 'Applied' });
  });

  it('derives review counts from actual item dispositions', () => {
    expect(
      tripImportDispositionCounts([
        { disposition: 'Ready' },
        { disposition: 'Warning' },
        { disposition: 'Blocked' },
        { disposition: 'Ready' },
      ] as never),
    ).toEqual({ ready: 2, warning: 1, blocked: 1 });
  });
});
