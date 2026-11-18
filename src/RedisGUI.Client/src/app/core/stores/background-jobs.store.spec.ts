import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject, of } from 'rxjs';
import { BackgroundJobsStore } from './background-jobs.store';
import { BackgroundJobsService, BackgroundJob, JobEvent } from '../services/background-jobs.service';

describe('BackgroundJobsStore', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('tracks live jobs, updates unread badge count, marks read, and scopes persisted hints by user', () => {
    const events = new Subject<JobEvent>();
    const initial: BackgroundJob = {
      jobId: 'job-1',
      connectionId: 'conn-1',
      kind: 'seed-data',
      displayName: 'Seed Sample Data',
      status: 'Queued',
      revision: 0,
      progressPercent: null,
      metrics: null,
      error: null,
      hasResult: false,
      updatedAt: '2026-01-01T00:00:00Z'
    };

    const jobsServiceMock = {
      kinds: jasmine.createSpy('kinds').and.returnValue(of([])),
      list: jasmine.createSpy('list').and.returnValue(of([initial])),
      start: jasmine.createSpy('start').and.returnValue(of(initial)),
      watch: jasmine.createSpy('watch').and.returnValue(events.asObservable()),
      status: jasmine.createSpy('status'),
      result: jasmine.createSpy('result').and.returnValue(of({ keysCreated: 10 })),
      cancel: jasmine.createSpy('cancel'),
      disconnect: jasmine.createSpy('disconnect')
    };

    const snackBarSpy = jasmine.createSpyObj<MatSnackBar>('MatSnackBar', ['open']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: BackgroundJobsService, useValue: jobsServiceMock },
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    });

    const store = TestBed.inject(BackgroundJobsStore);
    store.syncForUser('user-alpha');

    expect(store.visibleJobs().length).toBe(1);
    expect(store.activeCount()).toBe(1);
    expect(store.unreadCount()).toBe(0);

    events.next({
      connected: true,
      job: {
        ...initial,
        status: 'Completed',
        revision: 2,
        progressPercent: 100,
        hasResult: true
      }
    });

    expect(store.activeCount()).toBe(0);
    expect(store.unreadCount()).toBe(1);
    expect(store.badgeCount()).toBe(1);
    expect(snackBarSpy.open).toHaveBeenCalled();

    store.markAllRead();
    expect(store.unreadCount()).toBe(0);

    // Simulate page reload recovery where server no longer holds job-1
    jobsServiceMock.list.and.returnValue(of([]));
    store.resetSession(false);
    store.syncForUser('user-alpha');

    const recovered = store.visibleJobs();
    expect(recovered.length).toBe(1);
    expect(recovered[0]?.expiredReason).toContain('expired');

    // Switching user must not show user-alpha jobs
    store.syncForUser('user-beta');
    expect(store.visibleJobs().length).toBe(0);

    store.resetSession(true);
    expect(jobsServiceMock.disconnect).toHaveBeenCalled();
  });
});
