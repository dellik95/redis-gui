import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { MAT_DIALOG_DATA } from '@angular/material/dialog';
import { SlowlogDialogComponent } from './slowlog-dialog.component';

describe('slow log', () => {
  it('distinguishes empty results from a failed refresh and retries', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MAT_DIALOG_DATA, useValue: { connectionId: 'test' } }
      ]
    });
    const fixture = TestBed.createComponent(SlowlogDialogComponent);
    const dialog = fixture.componentInstance;
    const http = TestBed.inject(HttpTestingController);
    const get = () => http.expectOne(r => r.url.includes('slowlog'));
    fixture.detectChanges();
    get().flush({ entries: [], totalCount: 0 });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No Slow Queries Detected');
    dialog.loadSlowlog();
    fixture.detectChanges();
    get().flush({}, { status: 503, statusText: 'Unavailable' });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Could not load the slow log');
    dialog.loadSlowlog();
    fixture.detectChanges();
    get().flush({
      entries: [{ id: 1, timestamp: '2026-11-13', arguments: ['PING'] }],
      totalCount: 1
    });
    await fixture.whenStable();
    fixture.detectChanges();
    expect(dialog.entries()[0]?.arguments).toEqual(['PING']);
    http.verify();
  });
});
