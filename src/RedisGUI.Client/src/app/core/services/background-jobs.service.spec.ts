import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HubConnection } from '@microsoft/signalr';
import { BackgroundJob, BackgroundJobsService, JobEvent, JobHubFactory } from './background-jobs.service';

describe('BackgroundJobsService', () => {
  it('resubscribes and recovers status after reconnect without applying stale updates', async () => {
    const callbacks: Record<string, (value: BackgroundJob) => void> = {};
    let reconnect: (() => void) | undefined;
    let reconnecting: (() => void) | undefined;
    const hub = {
      on: (name: string, callback: (value: BackgroundJob) => void) => { callbacks[name] = callback; },
      onreconnecting: (callback: () => void) => { reconnecting = callback; },
      onreconnected: (callback: () => void) => { reconnect = callback; },
      onclose: () => {},
      start: jasmine.createSpy('start').and.returnValue(Promise.resolve()),
      invoke: jasmine.createSpy('invoke').and.returnValue(Promise.resolve()),
      stop: jasmine.createSpy('stop').and.returnValue(Promise.resolve())
    };
    TestBed.configureTestingModule({ providers: [
      provideHttpClient(), provideHttpClientTesting(),
      { provide: JobHubFactory, useValue: { create: () => hub as unknown as HubConnection } }
    ] });
    const service = TestBed.inject(BackgroundJobsService);
    const http = TestBed.inject(HttpTestingController);
    const events: JobEvent[] = [];
    const subscription = service.watch('job-1').subscribe(event => events.push(event));
    await new Promise(resolve => setTimeout(resolve, 0));
    const job: BackgroundJob = { jobId: 'job-1', connectionId: 'c', kind: 'load-test', status: 'Running', revision: 1,
      progressPercent: 10, metrics: null, error: null, hasResult: false, updatedAt: 'now' };
    http.expectOne('/api/v1/jobs/job-1').flush(job);
    await Promise.resolve();
    callbacks['JobUpdated']?.({ ...job, revision: 3, progressPercent: 30 });
    reconnecting?.();
    reconnect?.();
    await new Promise(resolve => setTimeout(resolve, 0));
    http.expectOne('/api/v1/jobs/job-1').flush({ ...job, revision: 2 });
    callbacks['JobCompleted']?.({ ...job, status: 'Completed', revision: 4, progressPercent: 100, hasResult: true });
    expect(hub.invoke).toHaveBeenCalledTimes(2);
    expect(events.filter(event => event.job).map(event => event.job!.revision)).toEqual([1, 3, 4]);
    expect(events.some(event => !event.connected)).toBeTrue();
    subscription.unsubscribe();
    expect(hub.stop).toHaveBeenCalled();
    http.verify();
  });
});
