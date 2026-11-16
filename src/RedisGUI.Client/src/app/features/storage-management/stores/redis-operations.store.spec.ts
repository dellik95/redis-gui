import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RedisOperationsStore } from './redis-operations.store';

describe('RedisOperationsStore bulk deletion', () => {
  beforeEach(() => TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] }));

  it('returns the server result instead of treating every selected key as deleted', () => {
    const store = TestBed.inject(RedisOperationsStore);
    const http = TestBed.inject(HttpTestingController);
    const onSuccess = jasmine.createSpy('onSuccess');
    store.deleteMultipleKeys({ connectionId: 'connection', keys: ['a', 'b'], database: 2, onSuccess });
    const request = http.expectOne(req => req.url === '/api/v1/storage/connection/keys/delete-multiple');
    const result = { deletedCount: 1, totalRequested: 2, failedKeys: ['b'] };
    request.flush(result);
    expect(onSuccess).toHaveBeenCalledWith(result);
    http.verify();
  });
});
