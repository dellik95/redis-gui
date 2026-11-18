import { TestBed } from '@angular/core/testing';
import { SideNavStore } from './side-nav.store';
import { ConnectionBrand } from '../../connection-brand';
import type { Connection } from '../../models/connection.type';

describe('SideNavStore and ConnectionBrand restoration', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  afterEach(() => {
    sessionStorage.clear();
  });

  it('uses /home for dashboard route, persists active connection per user, and displays Cluster badge only for Cluster topology', () => {
    const store = TestBed.inject(SideNavStore);
    expect(store.sideNavLinks()[0]?.href).toBe('/home');

    store.setUserScope('user-1');
    store.addConnectionRoutes('conn-1', 'Prod Cluster', 'Cluster');
    expect(store.connectionRoutes()[0]?.badge).toBe('Cluster');

    store.addConnectionRoutes('conn-2', 'Local Standalone', 'Standalone');
    expect(store.connectionRoutes()[0]?.badge).toBe('active');
    expect(store.getPersistedConnection('user-1')?.connectionId).toBe('conn-2');
    expect(store.getPersistedConnection('user-2')).toBeNull();

    store.resetSession(true);
    expect(store.getPersistedConnection('user-1')).toBeNull();
  });

  it('renders Cluster badge in ConnectionBrand only when serverMode is Cluster', async () => {
    await TestBed.configureTestingModule({
      imports: [ConnectionBrand]
    }).compileComponents();

    const fixture = TestBed.createComponent(ConnectionBrand);
    const baseConn: Connection = {
      id: 'c1',
      name: 'Redis',
      host: 'localhost',
      port: 6379,
      username: null,
      password: null,
      database: 0,
      serverMode: 'Unknown'
    };

    fixture.componentRef.setInput('connection', baseConn);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Cluster');

    fixture.componentRef.setInput('connection', { ...baseConn, serverMode: 'Standalone' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).not.toContain('Cluster');

    fixture.componentRef.setInput('connection', { ...baseConn, serverMode: 'Cluster' });
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Cluster');
  });
});
