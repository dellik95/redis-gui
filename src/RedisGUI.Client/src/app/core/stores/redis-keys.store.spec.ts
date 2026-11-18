import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RedisKeysStore, redisKeyIdentity } from './redis-keys.store';

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

  it('auto-continues empty scan pages when enabled, guards concurrent append, and keeps distinct binary keys', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const store = TestBed.inject(RedisKeysStore);
    const http = TestBed.inject(HttpTestingController);

    store.loadKeys({
      connectionId: 'cluster-1',
      pattern: '*',
      database: 0,
      autoContinueEmptyPages: true
    });

    const page1 = http.expectOne(req => req.url.endsWith('/cluster-1/key-scan') && !req.params.has('cursor'));
    page1.flush({ keys: [], nextCursor: 'hop-1', serverMode: 'Cluster' });

    const page2 = http.expectOne(req => req.params.get('cursor') === 'hop-1');
    page2.flush({
      keys: [
        { key: '\ufffd', keyBase64: 'gA==', isBinary: true, type: 'String', ttlSeconds: null },
        { key: '\ufffd', keyBase64: '/w==', isBinary: true, type: 'String', ttlSeconds: 30 }
      ],
      nextCursor: 'hop-2',
      serverMode: 'Cluster'
    });

    expect(store.serverMode()).toBe('Cluster');
    expect(store.keys().length).toBe(2);
    expect(store.keys().map(redisKeyIdentity)).toEqual(['b64:gA==', 'b64:/w==']);

    // Rapid duplicate appends must coalesce into a single request while in flight
    store.loadKeys({ connectionId: 'cluster-1', pattern: '*', database: 0, append: true });
    store.loadKeys({ connectionId: 'cluster-1', pattern: '*', database: 0, append: true });
    const appendReq = http.expectOne(req => req.params.get('cursor') === 'hop-2');
    appendReq.flush({ keys: [{ key: 'a/b', type: 'Hash', ttlSeconds: null }], nextCursor: null });

    // Query-safe route used for binary keys
    store.loadValue({ connectionId: 'cluster-1', key: '\ufffd', keyBase64: 'gA==', database: 0 });
    const valueReq = http.expectOne(req => req.url.includes('/storage/cluster-1/key?') && req.url.includes('keyBase64=gA%3D%3D'));
    valueReq.flush({
      key: '\ufffd',
      keyBase64: 'gA==',
      isBinary: true,
      type: 'string',
      value: 'hello',
      ttl: -1
    });
    expect(store.selectedKey()?.isBinary).toBeTrue();
    expect(store.selectedKey()?.keyBase64).toBe('gA==');

    http.verify();
  });
});
