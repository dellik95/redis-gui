
import { Component, inject, signal, ChangeDetectionStrategy, viewChild, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { RedisDataType } from '../../../../models/redis-keys.types';
import { JsonEditorComponent } from '../../../../shared/components/json-editor/json-editor.component';

export interface ValueEditorDialogData {
  key: string;
  type: RedisDataType;
  value: any;
  ttl?: number;
  isNew: boolean;
}

@Component({
  selector: 'app-value-editor-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatTabsModule,
    MatIconModule,
    MatChipsModule,
    MatSlideToggleModule,
    JsonEditorComponent
  ],
  templateUrl: './value-editor-dialog.component.html',
  styleUrls: ['./value-editor-dialog.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ValueEditorDialogComponent {
  private fb = inject(FormBuilder);
  public dialogRef = inject(MatDialogRef<ValueEditorDialogComponent>);
  public data = inject<ValueEditorDialogData>(MAT_DIALOG_DATA);

  RedisDataType = RedisDataType;
  
  form: FormGroup;
  selectedType = signal<RedisDataType>(RedisDataType.String);
  useJsonEditor = signal<boolean>(false);
  
  // For list/set editing
  listItems = signal<string[]>([]);
  newListItem = signal<string>('');
  
  // For hash editing
  hashFields = signal<Array<{key: string, value: string}>>([]);
  newHashKey = signal<string>('');
  newHashValue = signal<string>('');
  
  // For sorted set editing
  sortedSetMembers = signal<Array<{member: string, score: number}>>([]);
  newSortedMember = signal<string>('');
  newSortedScore = signal<number>(0);

  // JSON editor
  jsonEditorValue = signal<string>('');
  jsonEditorValid = signal<boolean>(true);
  jsonEditor = viewChild<JsonEditorComponent>('jsonEditor');

  // Computed value for JSON representation
  jsonRepresentation = computed(() => {
    try {
      const type = this.selectedType();
      switch (type) {
        case RedisDataType.String:
          return JSON.stringify({ value: this.form.get('stringValue')?.value || '' }, null, 2);
        case RedisDataType.List:
          return JSON.stringify({ items: this.listItems() }, null, 2);
        case RedisDataType.Set:
          return JSON.stringify({ members: this.listItems() }, null, 2);
        case RedisDataType.Hash:
          const hashObj: { [key: string]: string } = {};
          this.hashFields().forEach(field => {
            hashObj[field.key] = field.value;
          });
          return JSON.stringify({ fields: hashObj }, null, 2);
        case RedisDataType.SortedSet:
          const sortedSetObj: { [member: string]: number } = {};
          this.sortedSetMembers().forEach(item => {
            sortedSetObj[item.member] = item.score;
          });
          return JSON.stringify({ membersWithScores: sortedSetObj }, null, 2);
        default:
          return '{}';
      }
    } catch {
      return '{}';
    }
  });

  constructor() {
    this.selectedType.set(this.data.type || RedisDataType.String);
    this.form = this.fb.group({
      key: [{ value: this.data.key, disabled: !this.data.isNew }, Validators.required],
      type: [this.data.type || RedisDataType.String],
      stringValue: [this.data.value || ''],
      ttl: [this.data.ttl]
    });
    this.initializeValue();
    
    // Initialize JSON editor value
    this.jsonEditorValue.set(this.jsonRepresentation());
  }

  initializeValue(): void {
    if (!this.data.isNew && this.data.value) {
      switch (this.data.type) {
        case RedisDataType.List:
          this.listItems.set(Array.isArray(this.data.value) ? this.data.value : []);
          break;
        case RedisDataType.Hash:
          if (typeof this.data.value === 'object') {
            const fields = Object.entries(this.data.value).map(([key, value]) => ({
              key,
              value: String(value)
            }));
            this.hashFields.set(fields);
          }
          break;
        case RedisDataType.Set:
          this.listItems.set(Array.isArray(this.data.value) ? this.data.value : []);
          break;
        case RedisDataType.SortedSet:
          if (typeof this.data.value === 'object') {
            const members = Object.entries(this.data.value).map(([member, score]) => ({
              member,
              score: Number(score)
            }));
            this.sortedSetMembers.set(members);
          }
          break;
      }
    }
  }

  onTypeChange(type: RedisDataType): void {
    this.selectedType.set(type);
    this.jsonEditorValue.set(this.jsonRepresentation());
  }

  toggleJsonEditor(): void {
    const current = this.useJsonEditor();
    if (!current) {
      // Switching to JSON editor - update JSON value
      this.jsonEditorValue.set(this.jsonRepresentation());
    } else {
      // Switching from JSON editor - parse and update form
      this.parseJsonToForm();
    }
    this.useJsonEditor.set(!current);
  }

  onJsonEditorChange(value: string): void {
    this.jsonEditorValue.set(value);
  }

  onJsonEditorValid(isValid: boolean): void {
    this.jsonEditorValid.set(isValid);
  }

  parseJsonToForm(): void {
    try {
      const parsed = JSON.parse(this.jsonEditorValue());
      const type = this.selectedType();

      switch (type) {
        case RedisDataType.String:
          if (parsed.value !== undefined) {
            this.form.patchValue({ stringValue: parsed.value });
          }
          break;
        case RedisDataType.List:
          if (Array.isArray(parsed.items)) {
            this.listItems.set(parsed.items);
          }
          break;
        case RedisDataType.Set:
          if (Array.isArray(parsed.members)) {
            this.listItems.set(parsed.members);
          }
          break;
        case RedisDataType.Hash:
          if (parsed.fields && typeof parsed.fields === 'object') {
            const fields = Object.entries(parsed.fields).map(([key, value]) => ({
              key,
              value: String(value)
            }));
            this.hashFields.set(fields);
          }
          break;
        case RedisDataType.SortedSet:
          if (parsed.membersWithScores && typeof parsed.membersWithScores === 'object') {
            const members = Object.entries(parsed.membersWithScores).map(([member, score]) => ({
              member,
              score: Number(score)
            }));
            this.sortedSetMembers.set(members);
          }
          break;
      }
    } catch (error) {
      console.error('Error parsing JSON:', error);
    }
  }

  // List/Set operations
  addListItem(): void {
    const item = this.newListItem().trim();
    if (item) {
      this.listItems.update(items => [...items, item]);
      this.newListItem.set('');
    }
  }

  removeListItem(item: string): void {
    this.listItems.update(items => items.filter(x => x !== item));
  }

  // Hash operations
  addHashField(): void {
    const key = this.newHashKey().trim();
    const value = this.newHashValue().trim();
    if (key) {
      this.hashFields.update(fields => [...fields, { key, value }]);
      this.newHashKey.set('');
      this.newHashValue.set('');
    }
  }

  removeHashField(field: {key: string, value: string}): void {
    this.hashFields.update(fields => fields.filter(f => f.key !== field.key || f.value !== field.value));
  }

  // Sorted Set operations
  addSortedSetMember(): void {
    const member = this.newSortedMember().trim();
    const score = this.newSortedScore();
    if (member) {
      this.sortedSetMembers.update(members => [...members, { member, score }]);
      this.newSortedMember.set('');
      this.newSortedScore.set(0);
    }
  }

  removeSortedSetMember(member: {member: string, score: number}): void {
    this.sortedSetMembers.update(members => members.filter(m => m.member !== member.member || m.score !== member.score));
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onSave(): void {
    if (this.form.invalid) {
      return;
    }

    // If using JSON editor, parse it first
    if (this.useJsonEditor()) {
      if (!this.jsonEditorValid()) {
        return;
      }
      this.parseJsonToForm();
    }

    const formValue = this.form.getRawValue();
    let value: any;

    switch (this.selectedType()) {
      case RedisDataType.String:
        value = { value: formValue.stringValue };
        break;
      case RedisDataType.List:
        value = { items: this.listItems() };
        break;
      case RedisDataType.Set:
        value = { members: this.listItems() };
        break;
      case RedisDataType.Hash:
        const hashObj: { [key: string]: string } = {};
        this.hashFields().forEach(field => {
          hashObj[field.key] = field.value;
        });
        value = { fields: hashObj };
        break;
      case RedisDataType.SortedSet:
        const sortedSetObj: { [member: string]: number } = {};
        this.sortedSetMembers().forEach(item => {
          sortedSetObj[item.member] = item.score;
        });
        value = { membersWithScores: sortedSetObj };
        break;
      case RedisDataType.Stream:
        value = { entries: [] };
        break;
    }

    this.dialogRef.close({
      key: formValue.key,
      type: this.selectedType(),
      value,
      ttl: formValue.ttl
    });
  }
}
