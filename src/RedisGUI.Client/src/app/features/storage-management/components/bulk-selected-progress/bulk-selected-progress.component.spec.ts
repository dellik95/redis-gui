import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { BulkSelectedProgressComponent } from './bulk-selected-progress.component';

describe('BulkSelectedProgressComponent', () => {
  it('stops after the current batch and reports confirmed results', async () => {
    await TestBed.configureTestingModule({
      imports: [BulkSelectedProgressComponent],
      providers: [
        provideHttpClient(), provideHttpClientTesting(),
        { provide: MAT_DIALOG_DATA, useValue: { connectionId: 'connection', database: 2, operation: 'delete', keys: Array.from({ length: 150 }, (_, i) => `key-${i}`) } },
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close') } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(BulkSelectedProgressComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    const request = http.expectOne('/api/v1/storage/connection/keys/delete-multiple');
    expect(request.request.body.keys.length).toBe(100);
    fixture.componentInstance.stop();
    request.flush({ deletedCount: 99, totalRequested: 100, failedKeys: ['key-1'] });
    await fixture.whenStable();
    expect(fixture.componentInstance.processed()).toBe(100);
    expect(fixture.componentInstance.changed()).toBe(99);
    expect(fixture.componentInstance.failed()).toBe(1);
    expect(fixture.componentInstance.message()).toContain('Stopped');
    http.verify();
  });
});
