import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RedisKeysStore } from './redis-keys.store';

describe('incremental key browsing', () => {
  it('continues empty pages, deduplicates keys, and cancels stale scans', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const store = TestBed.inject(RedisKeysStore);
    const http = TestBed.inject(HttpTestingController);
    store.loadKeys({ connectionId: 'one', pattern: 'user:*', database: 1 });
    const first = http.expectOne(req => req.url.endsWith('/one/key-scan'));
    expect(first.request.params.get('database')).toBe('1');
    expect(first.request.params.get('pattern')).toBe('user:*');
    first.flush({ keys: [], nextCursor: 'first' });
    expect(store.nextCursor()).toBe('first');

    store.loadKeys({ connectionId: 'one', pattern: 'user:*', database: 1, append: true });
    const second = http.expectOne(req => req.params.get('cursor') === 'first');
    second.flush({
      keys: [{ key: 'user:1', type: 'String', ttlSeconds: 60 }],
      nextCursor: 'second'
    });
    store.loadKeys({ connectionId: 'one', pattern: 'user:*', database: 1, append: true });
    http
      .expectOne(req => req.params.get('cursor') === 'second')
      .flush({
        keys: [{ key: 'user:1', type: 'String', ttlSeconds: 50 }],
        nextCursor: null
      });
    expect(store.keys().length).toBe(1);
    expect(store.nextCursor()).toBeNull();

    store.loadKeys({ connectionId: 'one', pattern: 'old:*', database: 1 });
    const stale = http.expectOne(req => req.params.get('pattern') === 'old:*');
    store.loadKeys({ connectionId: 'one', pattern: 'new:*', database: 0 });
    expect(stale.cancelled).toBeTrue();
    http
      .expectOne(req => req.params.get('pattern') === 'new:*')
      .flush({ keys: [], nextCursor: null });
    expect(store.keys()).toEqual([]);
    http.verify();
  });
});
