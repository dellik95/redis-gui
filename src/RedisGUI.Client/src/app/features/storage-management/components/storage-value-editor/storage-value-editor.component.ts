import { Component, inject, signal, input, output, effect, ChangeDetectionStrategy, computed, viewChild, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { form, FormField, required, submit, readonly, disabled } from '@angular/forms/signals';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { RedisCliService } from '../../../../core/services/redis-cli.service';

import { JsonEditorComponent } from '../../../../shared/components/json-editor/json-editor.component';
import { RedisTypeHandlerRegistry, HashField, SortedSetMember, StreamVisualEntry } from '../../../../core/value-handlers';
import { RedisDataType, RedisValue } from '../../../../models/redis-keys.types';

import {
  TtlSelectorComponent,
  TypeBadgeComponent,
  StringEditorComponent,
  ListEditorComponent,
  SetEditorComponent,
  HashEditorComponent,
  ZsetEditorComponent,
  StreamEditorComponent
} from './components';
import { UiInputComponent, UiSelectComponent, UiButtonComponent } from '../../../../shared/ui';

export interface ValueEditorInput {
  key?: string;
  type?: RedisDataType;
  value?: RedisValue;
  ttl?: number;
  connectionId?: string;
  isNew?: boolean;
}

export interface ValueEditorOutput {
  key: string;
  type: RedisDataType;
  value: RedisValue;
  ttl?: number;
}

@Component({
  selector: 'app-storage-value-editor',
  standalone: true,
  imports: [
    FormField,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatSelectModule,
    MatIconModule,
    MatSlideToggleModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatDialogModule,
    JsonEditorComponent,
    TtlSelectorComponent,
    TypeBadgeComponent,
    StringEditorComponent,
    ListEditorComponent,
    SetEditorComponent,
    HashEditorComponent,
    ZsetEditorComponent,
    StreamEditorComponent,
    UiInputComponent,
    UiSelectComponent,
    UiButtonComponent
  ],
  templateUrl: './storage-value-editor.component.html',
  styleUrl: './storage-value-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageValueEditorComponent {
  private readonly handlerRegistry = inject(RedisTypeHandlerRegistry);
  private readonly dialogRef = inject(MatDialogRef<StorageValueEditorComponent>, { optional: true });
  private readonly dialogData = inject<{ data?: ValueEditorInput; connectionId?: string }>(MAT_DIALOG_DATA, { optional: true });
  private readonly cliService = inject(RedisCliService);
  private readonly destroyRef = inject(DestroyRef);

  /** Signal form model */
  readonly editorModel = signal({
    key: '',
    type: RedisDataType.String
  });

  /** Signal Form with declarative schema rules */
  readonly editorForm = form(this.editorModel, (s) => {
    required(s.key, { message: 'Key is required' });
    required(s.type, { message: 'Data type is required' });
    readonly(s.key, () => !this.isNewKey());
    disabled(s.type, () => !this.isNewKey());
  });

  readonly redisTypes = [
    { value: RedisDataType.String, label: 'String', icon: 'text_fields' },
    { value: RedisDataType.List, label: 'List', icon: 'format_list_numbered' },
    { value: RedisDataType.Set, label: 'Set', icon: 'category' },
    { value: RedisDataType.Hash, label: 'Hash', icon: 'data_object' },
    { value: RedisDataType.SortedSet, label: 'Sorted Set', icon: 'stacked_bar_chart' },
    { value: RedisDataType.Stream, label: 'Stream', icon: 'water' }
  ] as const;

  getTypeIcon(type: RedisDataType | string): string {
    const found = this.redisTypes.find(t => t.value === type);
    return found ? found.icon : 'category';
  }

  getTypeLabel(type: RedisDataType | string): string {
    const found = this.redisTypes.find(t => t.value === type);
    return found ? found.label : String(type || '');
  }

  // Inputs
  readonly data = input<ValueEditorInput | null>(null);
  readonly connectionId = input<string>('');
  readonly loading = input<boolean>(false);

  // Outputs
  readonly save = output<ValueEditorOutput>();
  readonly cancel = output<void>();

  // View queries
  readonly jsonEditor = viewChild<JsonEditorComponent>('jsonEditor');

  // State signals
  readonly selectedType = signal<RedisDataType>(RedisDataType.String);
  readonly useJsonEditor = signal<boolean>(false);
  readonly isNewKey = signal<boolean>(true);
  readonly validationErrors = signal<string[]>([]);
  readonly ttl = signal<number | null>(-1);
  readonly memoryUsageBytes = signal<number | null>(null);
  readonly isCheckingMemory = signal<boolean>(false);

  // Modular values signals
  readonly stringValue = signal<string>('');
  readonly listItems = signal<string[]>([]);
  readonly setMembers = signal<string[]>([]);
  readonly hashFields = signal<HashField[]>([]);
  readonly sortedSetMembers = signal<SortedSetMember[]>([]);
  readonly streamEntries = signal<StreamVisualEntry[]>([]);

  // Raw JSON mode
  readonly jsonEditorValue = signal<string>('{}');
  readonly jsonEditorValid = signal<boolean>(true);

  readonly RedisDataType = RedisDataType;

  readonly jsonRepresentation = computed(() => {
    try {
      const type = this.selectedType();
      const handler = this.handlerRegistry.getHandler(type);
      const visualValue = this.getVisualValue(type);
      return handler.toJson(visualValue);
    } catch {
      return '{}';
    }
  });

  readonly isValid = computed(() => {
    if (!this.editorForm().valid()) return false;
    if (this.useJsonEditor()) return this.jsonEditorValid();
    if (this.validationErrors().length > 0) return false;

    const formKey = this.editorModel().key;
    return !!(formKey && formKey.trim() && this.selectedType());
  });

  constructor() {
    effect(() => {
      const dData = this.dialogData;
      if (dData?.data && dData.connectionId) {
        this.loadData({
          ...dData.data,
          connectionId: dData.connectionId
        });
      } else {
        const inputData = this.data();
        if (inputData) {
          this.loadData(inputData);
        } else {
          this.reset();
        }
      }
    });

    effect(() => {
      const type = this.editorModel().type;
      if (type && type !== this.selectedType()) {
        this.selectedType.set(type);
        this.updateJsonEditor();
      }
    });

    effect(() => {
      const key = this.editorModel().key;
      if (key !== undefined) {
        if (this.useJsonEditor()) {
          this.validateJson();
        } else {
          this.validateValue();
        }
      }
    });

    effect(() => {
      const type = this.selectedType();
      if (!this.useJsonEditor()) {
        switch (type) {
          case RedisDataType.String: this.stringValue(); break;
          case RedisDataType.List: this.listItems(); break;
          case RedisDataType.Set: this.setMembers(); break;
          case RedisDataType.Hash: this.hashFields(); break;
          case RedisDataType.SortedSet: this.sortedSetMembers(); break;
          case RedisDataType.Stream: this.streamEntries(); break;
        }
        this.validateValue();
      }
    });
  }

  private normalizeType(type: RedisDataType | string | undefined | null): RedisDataType {
    if (!type) return RedisDataType.String;
    const lower = String(type).toLowerCase();
    switch (lower) {
      case 'list': return RedisDataType.List;
      case 'set': return RedisDataType.Set;
      case 'hash': return RedisDataType.Hash;
      case 'sortedset':
      case 'zset': return RedisDataType.SortedSet;
      case 'stream': return RedisDataType.Stream;
      default: return RedisDataType.String;
    }
  }

  private loadData(data: ValueEditorInput): void {
    const isNew = data.isNew ?? false;
    this.isNewKey.set(isNew);

    const type = this.normalizeType(data.type);
    this.selectedType.set(type);

    this.editorModel.set({
      key: data.key || '',
      type: type
    });

    this.ttl.set(data.ttl !== undefined && data.ttl !== null ? data.ttl : -1);

    if (data.value) {
      const handler = this.handlerRegistry.getHandler(type);
      const visualValue = handler.toVisual(data.value);
      this.applyVisualValue(type, visualValue);
    } else {
      const handler = this.handlerRegistry.getHandler(type);
      const defaultValue = handler.getDefaultValue();
      this.applyVisualValue(type, defaultValue);
    }

    if (!data.isNew && data.key) {
      this.checkMemoryUsage(data.key);
    } else {
      this.memoryUsageBytes.set(null);
    }

    this.updateJsonEditor();
  }

  private reset(): void {
    this.isNewKey.set(true);
    this.selectedType.set(RedisDataType.String);
    this.useJsonEditor.set(false);
    this.stringValue.set('');
    this.listItems.set([]);
    this.setMembers.set([]);
    this.hashFields.set([]);
    this.sortedSetMembers.set([]);
    this.streamEntries.set([]);
    this.validationErrors.set([]);
    this.ttl.set(-1);
    this.memoryUsageBytes.set(null);

    this.editorModel.set({
      key: '',
      type: RedisDataType.String
    });

    this.updateJsonEditor();
  }

  checkMemoryUsage(keyName?: string): void {
    const connId = this.connectionId() || this.dialogData?.connectionId;
    const key = keyName || this.editorModel().key;
    if (!connId || !key) return;

    this.isCheckingMemory.set(true);
    this.cliService.getKeyMemoryUsage(connId, key)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (res) => {
          this.memoryUsageBytes.set(res.memoryBytes);
          this.isCheckingMemory.set(false);
        },
        error: () => {
          this.isCheckingMemory.set(false);
        }
      });
  }

  formatBytes(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
  }

  private applyVisualValue(type: RedisDataType | string, visualValue: unknown): void {
    const t = this.normalizeType(type);
    switch (t) {
      case RedisDataType.String:
        this.stringValue.set(String(visualValue ?? ''));
        break;
      case RedisDataType.List:
        this.listItems.set(Array.isArray(visualValue) ? (visualValue as string[]) : []);
        break;
      case RedisDataType.Set:
        this.setMembers.set(Array.isArray(visualValue) ? (visualValue as string[]) : []);
        break;
      case RedisDataType.Hash:
        this.hashFields.set(Array.isArray(visualValue) ? (visualValue as HashField[]) : []);
        break;
      case RedisDataType.SortedSet:
        this.sortedSetMembers.set(Array.isArray(visualValue) ? (visualValue as SortedSetMember[]) : []);
        break;
      case RedisDataType.Stream:
        this.streamEntries.set(Array.isArray(visualValue) ? (visualValue as StreamVisualEntry[]) : []);
        break;
    }
  }

  private getVisualValue(type: RedisDataType | string): unknown {
    const t = this.normalizeType(type);
    switch (t) {
      case RedisDataType.String:
        return this.stringValue();
      case RedisDataType.List:
        return this.listItems();
      case RedisDataType.Set:
        return this.setMembers();
      case RedisDataType.Hash:
        return this.hashFields();
      case RedisDataType.SortedSet:
        return this.sortedSetMembers();
      case RedisDataType.Stream:
        return this.streamEntries();
      default:
        return null;
    }
  }

  private updateJsonEditor(): void {
    if (this.useJsonEditor()) {
      this.jsonEditorValue.set(this.jsonRepresentation());
    }
  }

  toggleJsonEditor(): void {
    const newState = !this.useJsonEditor();

    if (newState) {
      try {
        const type = this.selectedType();
        const handler = this.handlerRegistry.getHandler(type);
        const visualValue = this.getVisualValue(type);
        const jsonStr = handler.toJson(visualValue);
        this.jsonEditorValue.set(jsonStr);
      } catch (err) {
        console.warn('Failed to convert visual to JSON:', err);
        this.jsonEditorValue.set(this.jsonRepresentation());
      }
    } else {
      try {
        const handler = this.handlerRegistry.getHandler(this.selectedType());
        const visualValue = handler.fromJson(this.jsonEditorValue());
        this.applyVisualValue(this.selectedType(), visualValue);
        this.validationErrors.set([]);
      } catch (error: unknown) {
        const message = error instanceof Error ? error.message : 'Failed to parse JSON';
        this.validationErrors.set([message]);
      }
    }

    this.useJsonEditor.set(newState);
  }

  onJsonChange(value: string): void {
    this.jsonEditorValue.set(value);
    this.validateJson();
  }

  private validateJson(): void {
    try {
      const handler = this.handlerRegistry.getHandler(this.selectedType());
      handler.fromJson(this.jsonEditorValue());
      this.jsonEditorValid.set(true);
      this.validationErrors.set([]);
    } catch (error: unknown) {
      this.jsonEditorValid.set(false);
      const errorMessage = error instanceof Error ? error.message : 'Invalid JSON format';
      this.validationErrors.set([errorMessage]);
    }
  }

  formatJson(): void {
    if (this.useJsonEditor()) {
      this.jsonEditor()?.formatJson();
    }
  }

  private validateValue(): boolean {
    const type = this.selectedType();
    const handler = this.handlerRegistry.getHandler(type);
    const visualValue = this.getVisualValue(type);

    if (this.isNewKey()) {
      if (type === RedisDataType.String) {
        this.validationErrors.set([]);
        return true;
      }
      if (Array.isArray(visualValue) && visualValue.length === 0) {
        this.validationErrors.set([]);
        return true;
      }
      if (typeof visualValue === 'object' && visualValue !== null && Object.keys(visualValue).length === 0) {
        this.validationErrors.set([]);
        return true;
      }
    }

    const result = handler.validate(visualValue);
    this.validationErrors.set(result.errors);
    return result.valid;
  }

  onSave(event?: Event): void {
    if (event) event.preventDefault();
    if (!this.isValid()) return;

    submit(this.editorForm, async () => {
      const type = this.selectedType();
      const handler = this.handlerRegistry.getHandler(type);
      let visualValue: unknown;

      if (this.useJsonEditor()) {
        try {
          visualValue = handler.fromJson(this.jsonEditorValue());
          const validation = handler.validate(visualValue);
          if (!validation.valid) {
            this.validationErrors.set(validation.errors);
            return;
          }
        } catch (error: unknown) {
          const message = error instanceof Error ? error.message : 'Invalid JSON';
          this.validationErrors.set([message]);
          return;
        }
      } else {
        visualValue = this.getVisualValue(type);
      }

      let redisValue: RedisValue;
      switch (type) {
        case RedisDataType.String:
          redisValue = { value: String(visualValue ?? '') } as RedisValue;
          break;
        case RedisDataType.List:
          redisValue = { items: Array.isArray(visualValue) ? (visualValue as string[]) : [] } as RedisValue;
          break;
        case RedisDataType.Set:
          redisValue = { members: Array.isArray(visualValue) ? (visualValue as string[]) : [] } as RedisValue;
          break;
        case RedisDataType.Hash: {
          const hashObj: Record<string, string> = {};
          if (Array.isArray(visualValue)) {
            (visualValue as HashField[]).forEach((field: HashField) => {
              if (field.key) hashObj[field.key] = field.value;
            });
          }
          redisValue = { fields: hashObj } as RedisValue;
          break;
        }
        case RedisDataType.SortedSet: {
          const sortedSetObj: Record<string, number> = {};
          if (Array.isArray(visualValue)) {
            (visualValue as SortedSetMember[]).forEach((item: SortedSetMember) => {
              if (item.member) sortedSetObj[item.member] = item.score;
            });
          }
          redisValue = { membersWithScores: sortedSetObj } as RedisValue;
          break;
        }
        case RedisDataType.Stream: {
          const streamEntriesData = (Array.isArray(visualValue) ? (visualValue as StreamVisualEntry[]) : []).map(entry => {
            const vals: Record<string, string> = {};
            if (Array.isArray(entry.fields)) {
              entry.fields.forEach(f => {
                if (f.key && f.key.trim()) vals[f.key.trim()] = f.value;
              });
            }
            return { id: entry.id || '*', fields: vals, values: vals };
          });
          redisValue = { entries: streamEntriesData } as unknown as RedisValue;
          break;
        }
        default:
          redisValue = { value: String(visualValue ?? '') } as RedisValue;
      }

      const ttlVal = this.ttl();
      const finalTtl = ttlVal === null || ttlVal === undefined || ttlVal < 0 ? -1 : ttlVal;

      const output: ValueEditorOutput = {
        key: this.editorModel().key,
        type: type,
        value: redisValue,
        ttl: finalTtl
      };

      if (this.dialogRef) {
        this.dialogRef.close(output);
      } else {
        this.save.emit(output);
      }
    });
  }

  onCancel(): void {
    if (this.dialogRef) {
      this.dialogRef.close();
    } else {
      this.cancel.emit();
    }
  }
}
