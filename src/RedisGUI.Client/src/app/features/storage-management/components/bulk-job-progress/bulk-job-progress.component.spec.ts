import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { Subject, of } from 'rxjs';
import { BackgroundJob, BackgroundJobsService, JobEvent } from '../../../../core/services/background-jobs.service';
import { BulkJobProgressComponent } from './bulk-job-progress.component';

describe('BulkJobProgressComponent', () => {
  it('requests cancellation and displays the server’s partial result', async () => {
    const events = new Subject<JobEvent>();
    const initial: BackgroundJob = { jobId: 'job', connectionId: 'c', kind: 'bulk-keys', status: 'Queued', revision: 0,
      progressPercent: null, metrics: null, error: null, hasResult: false, updatedAt: 'now' };
    const result = { processed: 100, total: 150, changed: 99, unchanged: 0, failed: 1, failedKeys: ['key'],
      cancelled: true, mayHaveUnconfirmedChanges: false, error: null };
    const jobs = {
      start: jasmine.createSpy('start').and.returnValue(of(initial)),
      watch: () => events.asObservable(),
      cancel: jasmine.createSpy('cancel').and.returnValue(of({ ...initial, status: 'Cancelling', revision: 1 })),
      result: jasmine.createSpy('result').and.returnValue(of(result)),
      status: jasmine.createSpy('status')
    };
    await TestBed.configureTestingModule({
      imports: [BulkJobProgressComponent],
      providers: [
        { provide: BackgroundJobsService, useValue: jobs },
        { provide: MAT_DIALOG_DATA, useValue: { connectionId: 'c', database: 2, operation: 'delete', keys: ['a', 'b'] } },
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close'), disableClose: false } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(BulkJobProgressComponent);
    fixture.detectChanges();
    expect(jobs.start).toHaveBeenCalledWith('bulk-keys', 'c', jasmine.objectContaining({ database: 2, operation: 'delete' }));
    fixture.componentInstance.stop();
    expect(jobs.cancel).toHaveBeenCalledWith('job');
    events.next({ connected: true, job: { ...initial, status: 'Cancelled', revision: 2, hasResult: true } });
    expect(fixture.componentInstance.result()).toEqual(result);
    expect(fixture.componentInstance.changed()).toBe(99);
    expect(fixture.componentInstance.failed()).toBe(1);
  });
});
