import { Component, inject, signal, input, output, effect, ChangeDetectionStrategy, computed, viewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatTabsModule } from '@angular/material/tabs';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { RedisDataType } from '../../../../models/redis-keys.types';
import { JsonEditorComponent } from '../../../../shared/components/json-editor/json-editor.component';

export interface ValueEditorData {
  key: string;
  type: RedisDataType;
  value: any;
  ttl?: number;
}

@Component({
  selector: 'app-inline-value-editor',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatTabsModule,
    MatIconModule,
    MatChipsModule,
    MatSlideToggleModule,
    MatCardModule,
    MatProgressSpinnerModule,
    JsonEditorComponent
  ],
  templateUrl: './inline-value-editor.component.html',
  styleUrls: ['./inline-value-editor.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InlineValueEditorComponent {
  private fb = inject(FormBuilder);

  // Inputs
  selectedKey = input<ValueEditorData | null>(null);
  loading = input<boolean>(false);

  // Outputs
  save = output<ValueEditorData>();
  cancel = output<void>();

  RedisDataType = RedisDataType;

  form: FormGroup;
  selectedType = signal<RedisDataType>(RedisDataType.String);
  useJsonEditor = signal<boolean>(false);

  // For list/set editing
  listItems = signal<string[]>([]);
  newListItem = signal<string>('');

  // For hash editing
  hashFields = signal<Array<{ key: string, value: string }>>([]);
  newHashKey = signal<string>('');
  newHashValue = signal<string>('');

  // For sorted set editing
  sortedSetMembers = signal<Array<{ member: string, score: number }>>([]);
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
    this.form = this.fb.group({
      key: ['', Validators.required],
      stringValue: [''],
      ttl: [null]
    });

    effect(() => {
      const data = this.selectedKey();
      if (data) {
        this.loadData(data);
      }
    });
  }

  private loadData(data: ValueEditorData): void {
    this.form.patchValue({
      key: data.key,
      ttl: data.ttl
    });

    this.selectedType.set(data.type);

    // Initialize based on type
    switch (data.type) {
      case RedisDataType.String:
        this.form.patchValue({ stringValue: data.value || '' });
        break;

      case RedisDataType.List:
      case RedisDataType.Set:
        const items = Array.isArray(data.value) ? data.value : [];
        this.listItems.set(items.map(String));
        break;

      case RedisDataType.Hash:
        if (typeof data.value === 'object' && data.value !== null) {
          const fields = Object.entries(data.value).map(([key, value]) => ({
            key,
            value: String(value)
          }));
          this.hashFields.set(fields);
        }
        break;

      case RedisDataType.SortedSet:
        if (Array.isArray(data.value)) {
          const members = data.value.map((item: any) => ({
            member: String(item.member || item.value || item),
            score: Number(item.score || 0)
          }));
          this.sortedSetMembers.set(members);
        }
        break;
    }

    // Set JSON editor value
    this.jsonEditorValue.set(this.jsonRepresentation());
  }

  toggleJsonEditor(): void {
    const newState = !this.useJsonEditor();
    
    if (newState) {
      // Switching TO JSON editor - populate with current data
      this.jsonEditorValue.set(this.jsonRepresentation());
    } else {
      // Switching FROM JSON editor - parse JSON back to visual editor
      try {
        const parsed = JSON.parse(this.jsonEditorValue());
        this.applyJsonToVisualEditor(parsed);
      } catch (error) {
        console.error('Failed to parse JSON', error);
      }
    }
    
    this.useJsonEditor.set(newState);
  }

  private applyJsonToVisualEditor(parsed: any): void {
    const type = this.selectedType();
    
    switch (type) {
      case RedisDataType.String:
        if (parsed.value !== undefined) {
          this.form.patchValue({ stringValue: String(parsed.value) });
        }
        break;

      case RedisDataType.List:
        if (Array.isArray(parsed.items)) {
          this.listItems.set(parsed.items.map(String));
        }
        break;

      case RedisDataType.Set:
        if (Array.isArray(parsed.members)) {
          this.listItems.set(parsed.members.map(String));
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
  }

  onJsonChange(value: string): void {
    this.jsonEditorValue.set(value);
  }

  // List/Set operations
  addListItem(): void {
    const item = this.newListItem().trim();
    if (item) {
      this.listItems.update(items => [...items, item]);
      this.newListItem.set('');
    }
  }

  removeListItem(index: number): void {
    this.listItems.update(items => items.filter((_, i) => i !== index));
  }

  // Hash operations
  addHashField(): void {
    const key = this.newHashKey().trim();
    if (key) {
      this.hashFields.update(fields => [...fields, { key, value: this.newHashValue() }]);
      this.newHashKey.set('');
      this.newHashValue.set('');
    }
  }

  removeHashField(index: number): void {
    this.hashFields.update(fields => fields.filter((_, i) => i !== index));
  }

  updateHashField(index: number, key: string, value: string): void {
    this.hashFields.update(fields => {
      const updated = [...fields];
      updated[index] = { key, value };
      return updated;
    });
  }

  // Sorted Set operations
  addSortedSetMember(): void {
    const member = this.newSortedMember().trim();
    if (member) {
      this.sortedSetMembers.update(members => [
        ...members,
        { member, score: this.newSortedScore() }
      ]);
      this.newSortedMember.set('');
      this.newSortedScore.set(0);
    }
  }

  removeSortedSetMember(index: number): void {
    this.sortedSetMembers.update(members => members.filter((_, i) => i !== index));
  }

  updateSortedSetScore(index: number, score: number): void {
    this.sortedSetMembers.update(members => {
      const updated = [...members];
      updated[index].score = score;
      return updated;
    });
  }

  formatJson(): void {
    if (this.useJsonEditor()) {
      this.jsonEditor()?.formatJson();
    }
  }

  onSave(): void {
    if (this.form.invalid) {
      return;
    }

    const key = this.form.value.key;
    const ttl = this.form.value.ttl;
    const type = this.selectedType();
    let value: any;

    if (this.useJsonEditor()) {
      // Parse from JSON editor
      try {
        const parsed = JSON.parse(this.jsonEditorValue());
        value = this.extractValueFromJson(parsed, type);
      } catch {
        return; // Invalid JSON
      }
    } else {
      // Get from visual editor
      value = this.getValueFromVisualEditor(type);
    }

    this.save.emit({ key, type, value, ttl });
  }

  private extractValueFromJson(parsed: any, type: RedisDataType): any {
    switch (type) {
      case RedisDataType.String:
        return parsed.value || '';
      case RedisDataType.List:
        return parsed.items || [];
      case RedisDataType.Set:
        return parsed.members || [];
      case RedisDataType.Hash:
        return parsed.fields || {};
      case RedisDataType.SortedSet:
        if (parsed.membersWithScores) {
          return Object.entries(parsed.membersWithScores).map(([member, score]) => ({
            member,
            score: Number(score)
          }));
        }
        return [];
      default:
        return null;
    }
  }

  private getValueFromVisualEditor(type: RedisDataType): any {
    switch (type) {
      case RedisDataType.String:
        return this.form.value.stringValue || '';

      case RedisDataType.List:
      case RedisDataType.Set:
        return this.listItems();

      case RedisDataType.Hash:
        const hashObj: { [key: string]: string } = {};
        this.hashFields().forEach(field => {
          hashObj[field.key] = field.value;
        });
        return hashObj;

      case RedisDataType.SortedSet:
        return this.sortedSetMembers();

      default:
        return null;
    }
  }

  onCancel(): void {
    this.cancel.emit();
  }
}
