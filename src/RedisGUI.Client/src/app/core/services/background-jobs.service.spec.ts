import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { HubConnection } from '@microsoft/signalr';
import { BackgroundJob, BackgroundJobsService, JobEvent, JobHubFactory } from './background-jobs.service';

interface MockHubControl {
  callbacks: Record<string, (...args: unknown[]) => void>;
  reconnect?: () => void;
  reconnecting?: () => void;
  hub: {
    on: (name: string, callback: (...args: unknown[]) => void) => void;
    onreconnecting: (callback: () => void) => void;
    onreconnected: (callback: () => void) => void;
    onclose: () => void;
    start: jasmine.Spy<() => Promise<void>>;
    invoke: jasmine.Spy<(...args: unknown[]) => Promise<void>>;
    stop: jasmine.Spy<() => Promise<void>>;
  };
}

function createMockHub(startImpl?: () => Promise<void>): MockHubControl {
  const control: MockHubControl = {
    callbacks: {},
    hub: {
      on: (name, callback) => {
        control.callbacks[name] = callback;
      },
      onreconnecting: callback => {
        control.reconnecting = callback;
      },
      onreconnected: callback => {
        control.reconnect = callback;
      },
      onclose: () => {},
      start: jasmine.createSpy('start').and.callFake(startImpl ?? (() => Promise.resolve())),
      invoke: jasmine.createSpy('invoke').and.returnValue(Promise.resolve()),
      stop: jasmine.createSpy('stop').and.returnValue(Promise.resolve())
    }
  };
  return control;
}

describe('BackgroundJobsService', () => {
  it('resubscribes and recovers status after reconnect without applying stale updates', async () => {
    const mock = createMockHub();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: JobHubFactory, useValue: { create: () => mock.hub as unknown as HubConnection } }
      ]
    });
    const service = TestBed.inject(BackgroundJobsService);
    const http = TestBed.inject(HttpTestingController);
    const events: JobEvent[] = [];
    const subscription = service.watch('job-1').subscribe(event => events.push(event));
    await new Promise(resolve => setTimeout(resolve, 0));
    const job: BackgroundJob = {
      jobId: 'job-1',
      connectionId: 'c',
      kind: 'load-test',
      status: 'Running',
      revision: 1,
      progressPercent: 10,
      metrics: null,
      error: null,
      hasResult: false,
      updatedAt: 'now'
    };
    http.expectOne('/api/v1/jobs/job-1').flush(job);
    await Promise.resolve();
    mock.callbacks['JobUpdated']?.({ ...job, revision: 3, progressPercent: 30 });
    mock.reconnecting?.();
    mock.reconnect?.();
    await new Promise(resolve => setTimeout(resolve, 0));
    http.expectOne('/api/v1/jobs/job-1').flush({ ...job, revision: 2 });
    mock.callbacks['JobCompleted']?.({
      ...job,
      status: 'Completed',
      revision: 4,
      progressPercent: 100,
      hasResult: true
    });
    expect(mock.hub.invoke).toHaveBeenCalledTimes(2);
    expect(events.filter(event => event.job).map(event => event.job!.revision)).toEqual([1, 3, 4]);
    expect(events.some(event => !event.connected)).toBeTrue();
    subscription.unsubscribe();
    expect(mock.hub.stop).toHaveBeenCalled();
    http.verify();
  });

  it('shares one hub connection across multiple watched jobs, retries transient start failure, and isolates AccessRevoked', async () => {
    jasmine.clock().install();
    try {
      let attempts = 0;
      const mock = createMockHub(() => {
        attempts++;
        return attempts === 1 ? Promise.reject(new Error('transient')) : Promise.resolve();
      });
      const factorySpy = jasmine.createSpy('create').and.returnValue(mock.hub as unknown as HubConnection);

      TestBed.configureTestingModule({
        providers: [
          provideHttpClient(),
          provideHttpClientTesting(),
          { provide: JobHubFactory, useValue: { create: factorySpy } }
        ]
      });

      const service = TestBed.inject(BackgroundJobsService);
      const http = TestBed.inject(HttpTestingController);
      const eventsA: JobEvent[] = [];
      const eventsB: JobEvent[] = [];

      const subA = service.watch('job-a').subscribe(e => eventsA.push(e));
      const subB = service.watch('job-b').subscribe(e => eventsB.push(e));

      expect(factorySpy).toHaveBeenCalledTimes(1);

      const flushMicrotasks = async (count = 12) => {
        for (let i = 0; i < count; i++) {
          await Promise.resolve();
        }
      };

      await flushMicrotasks();
      jasmine.clock().tick(600);
      await flushMicrotasks();

      http.expectOne('/api/v1/jobs/job-a').flush({
        jobId: 'job-a',
        connectionId: 'c1',
        kind: 'redis-export',
        status: 'Running',
        revision: 1,
        progressPercent: 25,
        metrics: null,
        error: null,
        hasResult: false,
        updatedAt: 'now'
      });
      http.expectOne('/api/v1/jobs/job-b').flush({
        jobId: 'job-b',
        connectionId: 'c2',
        kind: 'seed-data',
        status: 'Running',
        revision: 1,
        progressPercent: 50,
        metrics: null,
        error: null,
        hasResult: false,
        updatedAt: 'now'
      });
      await Promise.resolve();

      mock.callbacks['AccessRevoked']?.('job-a');
      expect(eventsA.some(e => e.revoked === true)).toBeTrue();
      expect(eventsB.some(e => e.revoked === true)).toBeFalse();

      subA.unsubscribe();
      expect(mock.hub.stop).not.toHaveBeenCalled();
      subB.unsubscribe();
      expect(mock.hub.stop).toHaveBeenCalled();
      http.verify();
    } finally {
      jasmine.clock().uninstall();
    }
  });
});
