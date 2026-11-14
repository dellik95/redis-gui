import { signal } from '@angular/core';
import { fakeAsync, TestBed, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { ConnectionsStore } from '../../core/stores/connections.store';
import { InfoComponent } from './info.component';

describe('server information', () => {
  it('polls the selected connection only on Overview and preserves the config search', fakeAsync(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ connectionId: 'one' })),
            queryParamMap: of(convertToParamMap({}))
          }
        },
        {
          provide: ConnectionsStore,
          useValue: { connections: signal([]), loadConnections: () => {} }
        }
      ]
    });
    TestBed.overrideComponent(InfoComponent, { set: { template: '', imports: [] } });
    const fixture = TestBed.createComponent(InfoComponent);
    const http = TestBed.inject(HttpTestingController);
    fixture.detectChanges();
    http.expectOne('/api/v1/connections/one/info').flush({});
    http.expectOne('/api/v1/connections/one/config').flush({ parameters: [] });
    tick(5000);
    http.expectOne('/api/v1/connections/one/info').flush({});
    fixture.componentInstance.selectedTab.set(1);
    fixture.detectChanges();
    tick(5000);
    http.expectNone('/api/v1/connections/one/info');
    fixture.componentInstance.onConfigSearch('memory');
    tick(350);
    let config = http.expectOne(request => request.url.endsWith('/config'));
    expect(config.request.params.get('pattern')).toBe('*memory*');
    config.flush({ parameters: [] });
    fixture.componentInstance.loadData();
    http.expectOne('/api/v1/connections/one/info').flush({});
    config = http.expectOne(request => request.url.endsWith('/config'));
    expect(config.request.params.get('pattern')).toBe('*memory*');
    config.flush({ parameters: [] });
    fixture.componentInstance.selectedTab.set(0);
    fixture.detectChanges();
    fixture.destroy();
    tick(5000);
    http.expectNone('/api/v1/connections/one/info');
    http.verify();
  }));
});
