import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { EditKeyComponent } from './edit-key.component';
import { RedisDataType } from '../../../../models/redis-keys.types';

describe('Redis key editing', () => {
  it('cancels an overlapping read, encodes the key, and acknowledges only a successful write', async () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    const fixture = TestBed.createComponent(EditKeyComponent);
    const editor = fixture.componentInstance;
    const http = TestBed.inject(HttpTestingController);
    fixture.componentRef.setInput('connectionId', 'test');
    fixture.componentRef.setInput('selectedKey', { key: 'old', type: 'string' });
    fixture.detectChanges();
    const old = http.expectOne(r => r.url.endsWith('/old'));
    fixture.componentRef.setInput('selectedKey', { key: 'a/b', type: 'string' });
    fixture.detectChanges();
    expect(old.cancelled).toBeTrue();
    const read = http.expectOne(r => r.url.endsWith('/a%2Fb'));
    expect(read.request.params.get('database')).toBe('0');
    read.flush({ key: 'a/b', type: 'String', value: { value: 'before' } });
    await fixture.whenStable();
    expect(editor.editorData()?.value).toEqual({ value: 'before' });
    const saved = jasmine.createSpy('saved');
    editor.saved.subscribe(saved);
    editor.onSave({ key: 'a/b', type: RedisDataType.String, value: { value: 'after' } });
    const put = http.expectOne(r => r.method === 'PUT' && r.url.includes('/keys/a%2Fb'));
    expect(put.request.body.value).toEqual({ value: 'after' });
    expect(saved).not.toHaveBeenCalled();
    put.flush({});
    expect(saved).toHaveBeenCalledTimes(1);
    expect(editor.isSaving()).toBeFalse();
    fixture.destroy();
    http.verify();
  });
});
