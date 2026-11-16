import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';
import { BulkDeleteProgressComponent } from './bulk-delete-progress.component';

describe('BulkDeleteProgressComponent', () => {
  it('shows the final count when a pattern deletion is stopped', async () => {
    const text = 'event: progress\ndata: {"deletedCount":2}\n\nevent: complete\ndata: {"deletedCount":3,"cancelled":true,"error":null}\n\n';
    const stream = new ReadableStream<Uint8Array>({ start(controller) { controller.enqueue(new TextEncoder().encode(text)); controller.close(); } });
    spyOn(window, 'fetch').and.returnValue(Promise.resolve(new Response(stream, { status: 200 })));
    await TestBed.configureTestingModule({
      imports: [BulkDeleteProgressComponent],
      providers: [
        provideHttpClient(),
        { provide: MAT_DIALOG_DATA, useValue: { connectionId: 'connection', database: 2, pattern: 'user:*' } },
        { provide: MatDialogRef, useValue: { close: jasmine.createSpy('close') } },
        { provide: TokenStorageService, useValue: { token: () => null, isTokenExpired: () => false } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(BulkDeleteProgressComponent);
    fixture.detectChanges();
    for (let attempt = 0; attempt < 100 && fixture.componentInstance.running(); attempt++) {
      await new Promise(resolve => setTimeout(resolve, 10));
    }
    expect(fixture.componentInstance.deleted()).toBe(3);
    expect(fixture.componentInstance.running()).toBeFalse();
    expect(fixture.componentInstance.message()).toContain('Stopped');
  });
});
