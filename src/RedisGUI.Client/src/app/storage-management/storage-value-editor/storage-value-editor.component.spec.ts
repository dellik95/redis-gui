import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StorageValueEditorComponent } from './storage-value-editor.component';

describe('StorageValueEditorComponent', () => {
  let component: StorageValueEditorComponent;
  let fixture: ComponentFixture<StorageValueEditorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StorageValueEditorComponent]
    })
    .compileComponents();

    fixture = TestBed.createComponent(StorageValueEditorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
