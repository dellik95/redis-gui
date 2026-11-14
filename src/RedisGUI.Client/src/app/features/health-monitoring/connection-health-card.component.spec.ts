import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ConnectionHealthCardComponent } from './connection-health-card.component';

describe('per-connection health polling', () => {
  it('isolates card results, clears the timer and cancels an outstanding read on teardown', async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const http = TestBed.inject(HttpTestingController);
    const start = spyOn(window, 'setInterval').and.returnValue(123);
    const stop = spyOn(window, 'clearInterval').and.callThrough();
    const card = (id: string, active: boolean) => {
      const fixture = TestBed.createComponent(ConnectionHealthCardComponent);
      fixture.componentRef.setInput('connectionId', id);
      fixture.componentRef.setInput('pollingActive', active);
      fixture.componentRef.setInput('refreshIntervalMs', 12345);
      fixture.detectChanges();
      return fixture;
    };
    const first = card('one', true);
    const second = card('two', false);
    const timer = start.calls.all().find(call => call.args[1] === 12345)!;
    const one = http.expectOne('/api/v1/connections/one/health');
    const two = http.expectOne('/api/v1/connections/two/health');
    two.flush({ status: 'Degraded', responseTimeMs: 20 });
    one.flush({ status: 'Healthy', responseTimeMs: 2 });
    await new Promise(resolve => setTimeout(resolve, 0));
    TestBed.tick();
    expect(first.componentInstance.statusClass()).toBe('healthy');
    expect(second.componentInstance.statusClass()).toBe('degraded');
    const poll = timer.args[0];
    if (typeof poll !== 'function') throw new Error('Expected polling callback');
    poll();
    TestBed.tick();
    await Promise.resolve();
    const pending = http.expectOne('/api/v1/connections/one/health');
    first.destroy();
    expect(stop).toHaveBeenCalledWith(timer.returnValue);
    expect(pending.cancelled).toBeTrue();
    http.expectNone('/api/v1/connections/two/health');
    http.verify();
  });
});
