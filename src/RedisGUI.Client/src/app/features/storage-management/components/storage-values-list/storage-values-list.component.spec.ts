import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { StorageValuesListComponent } from './storage-values-list.component';
import { ConnectionsStore } from '../../../../core/stores/connections.store';

describe('StorageValuesListComponent virtual scroll & Cluster/Binary rendering', () => {
  it('virtualizes large key lists via cdkVirtualFor, renders BIN badge, locks Cluster to DB 0, and auto-loads on scroll', async () => {
    await TestBed.configureTestingModule({
      imports: [StorageValuesListComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MatSnackBar, useValue: { open: () => undefined } }
      ]
    }).compileComponents();

    const connectionsStore = TestBed.inject(ConnectionsStore);
    connectionsStore.selectConnection({
      id: 'conn-cluster',
      name: 'Cluster',
      host: '127.0.0.1',
      port: 7000,
      username: null,
      password: null,
      database: 3,
      serverMode: 'Cluster'
    });

    const fixture = TestBed.createComponent(StorageValuesListComponent);
    const hostEl = fixture.nativeElement as HTMLElement;
    hostEl.style.height = '420px';
    hostEl.style.display = 'flex';
    hostEl.style.flexDirection = 'column';

    fixture.componentRef.setInput('connectionId', 'conn-cluster');
    fixture.detectChanges();

    const http = TestBed.inject(HttpTestingController);
    const scanReq = http.expectOne(req => req.url.endsWith('/conn-cluster/key-scan'));
    // Cluster must enforce DB 0 even if connection metadata had stale DB 3
    expect(scanReq.request.params.get('database')).toBe('0');

    const batch = Array.from({ length: 120 }, (_, i) => ({
      key: i === 0 ? 'bin\ufffdkey' : `key:${i.toString().padStart(3, '0')}`,
      keyBase64: i === 0 ? 'Ymlu/2tleQ==' : null,
      isBinary: i === 0,
      type: 'String',
      ttlSeconds: null
    }));

    scanReq.flush({
      keys: batch,
      nextCursor: 'cursor-next',
      serverMode: 'Cluster'
    });
    http.expectOne(req => req.url.endsWith('/conn-cluster/keyspace')).flush({ databases: { 0: 120 } });

    fixture.detectChanges();
    await new Promise(resolve => setTimeout(resolve, 60));
    fixture.detectChanges();

    expect(fixture.componentInstance.isCluster()).toBeTrue();
    expect(fixture.componentInstance.selectedDb()).toBe(0);

    const rows = hostEl.querySelectorAll('.vtable-row');
    expect(rows.length).toBeGreaterThan(0);
    expect(rows.length).toBeLessThan(120);
    expect(hostEl.textContent).toContain('DB 0 · Cluster');
    expect(hostEl.textContent).toContain('BIN');

    // Scroll near bottom triggers incremental cursor append
    fixture.componentInstance.onScrolledIndexChange(100);
    const appendReq = http.expectOne(
      req => req.url.endsWith('/conn-cluster/key-scan') && req.params.get('cursor') === 'cursor-next'
    );
    appendReq.flush({ keys: [], nextCursor: null, serverMode: 'Cluster' });

    http.verify();
  });
});
