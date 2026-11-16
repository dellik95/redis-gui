import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RedisCliService } from './redis-cli.service';
import { TokenStorageService } from './auth/token-storage.service';

describe('streaming load tests', () => {
  it('sends the bearer token and submits the stop ID in the request body', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: TokenStorageService,
          useValue: { token: () => 'token', isTokenExpired: () => false }
        }
      ]
    });
    const service = TestBed.inject(RedisCliService);
    const http = TestBed.inject(HttpTestingController);
    const fetchSpy = spyOn(window, 'fetch').and.returnValue(new Promise<Response>(() => {}));

    service.streamLoadTest(
      'connection',
      {},
      'test-id',
      () => {},
      () => {},
      () => {}
    );
    const options = fetchSpy.calls.mostRecent().args[1] as RequestInit;
    expect((options.headers as Record<string, string>)['Authorization']).toBe('Bearer token');

    service.stopLoadTest('connection', 'test-id').subscribe();
    const stop = http.expectOne('/api/v1/storage/connection/load-test/stop');
    expect(stop.request.body).toEqual({ testId: 'test-id' });
    stop.flush({ success: true, message: 'stopped' });
    http.verify();
  });
});
