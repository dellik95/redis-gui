import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ExportService } from './export.service';
import { ImportService } from './import.service';

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
    service.downloadExport('connection', 'json', 'user:*', 2).subscribe();
    const request = http.expectOne(req => req.url === '/api/v1/storage/connection/export');
    expect(request.request.params.get('database')).toBe('2');
    expect(request.request.params.get('pattern')).toBe('user:*');
    request.flush(new Blob(['[]'], { type: 'application/json' }));
    http.verify();
  });

  it('sends file content and selected database in the import request', async () => {
    const service = TestBed.inject(ImportService);
    const http = TestBed.inject(HttpTestingController);
    const file = new File(['[]'], 'data.json', { type: 'application/json' });
    spyOn(file, 'text').and.returnValue(Promise.resolve('[]'));
    service.importData('connection', file, 2).subscribe();
    await Promise.resolve();
    const request = http.expectOne('/api/v1/storage/connection/import');
    expect(request.request.body).toEqual({ data: '[]', format: 'json', overwrite: false, database: 2 });
    request.flush({ importedCount: 0, skippedCount: 0, failedCount: 0, failedKeys: [] });
    http.verify();
  });
});
