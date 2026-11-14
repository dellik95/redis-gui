import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthStore } from '../../core/stores/auth.store';
import { AuditLogsComponent } from './audit-logs.component';

describe('audit filters', () => {
  it('requests once per initial load, apply, page and reset, and keeps drafts local', async () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthStore, useValue: { isAdmin: signal(true) } }
      ]
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(AuditLogsComponent);
    const component = fixture.componentInstance;
    function expectPage(skip: number, entity: string | null) {
      const request = http.expectOne(r => r.url === '/api/v1/audit');
      expect(request.request.params.get('skip')).toBe(String(skip));
      expect(request.request.params.get('entityType')).toBe(entity);
      request.flush({ items: [], totalCount: 110, skip, take: 50 });
      TestBed.tick();
    }
    fixture.detectChanges();
    expectPage(0, null);
    component.filterModel.update(model => ({ ...model, entityType: 'Redis' }));
    TestBed.tick();
    http.expectNone(r => r.url === '/api/v1/audit');
    component.applyFilters();
    await Promise.resolve();
    expectPage(0, 'Redis');
    component.filterModel.update(model => ({ ...model, entityType: 'Connection' }));
    component.goToPage(2);
    expectPage(50, 'Redis');
    component.filterModel.update(model => ({ ...model, entityId: 'audit' }));
    component.applyFilters();
    await Promise.resolve();
    http.expectNone(r => r.url === '/api/v1/audit');
    component.loadAuditLogs();
    expectPage(50, 'Redis');
    fixture.nativeElement.querySelector('button.btn-ghost').click();
    await Promise.resolve();
    expectPage(0, null);
    http.verify();
  });
});
