import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ExportService } from './export.service';
import { ImportService } from './import.service';
import { BackgroundJobsService } from '../../../core/services/background-jobs.service';

describe('Redis data transfer', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  });

  it('exports the selected database and pattern', () => {
    const service = TestBed.inject(ExportService);
    const http = TestBed.inject(HttpTestingController);
    spyOn(URL, 'createObjectURL').and.returnValue('blob:test');
    spyOn(URL, 'revokeObjectURL');
    spyOn(HTMLAnchorElement.prototype, 'click');
    TestBed.inject(BackgroundJobsService).start('redis-export', 'connection', { database: 2, pattern: 'user:*', format: 'json' }).subscribe();
    const start = http.expectOne('/api/v1/jobs');
    expect(start.request.body.parameters).toEqual({ database: 2, pattern: 'user:*', format: 'json' });
    start.flush({ jobId: 'job' });
    service.downloadExport('job', 'json').subscribe();
    const download = http.expectOne('/api/v1/jobs/job/result');
    download.flush(new Blob(['[]'], { type: 'application/json' }));
    http.verify();
  });

  it('sends file content and selected database in the import request', async () => {
    const service = TestBed.inject(ImportService);
    const http = TestBed.inject(HttpTestingController);
    const file = new File(['[]'], 'data.json', { type: 'application/json' });
    spyOn(file, 'text').and.returnValue(Promise.resolve('[]'));
    service.importData('connection', file, 2).subscribe();
    await Promise.resolve();
    const request = http.expectOne('/api/v1/jobs');
    expect(request.request.body).toEqual({ kind: 'redis-import', connectionId: 'connection', parameters: { data: '[]', format: 'json', overwrite: false, database: 2 } });
    request.flush({ jobId: 'job' });
    http.verify();
  });
});
