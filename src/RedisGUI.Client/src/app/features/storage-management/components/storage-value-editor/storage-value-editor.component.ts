import { Component, inject, signal, input, output, effect, ChangeDetectionStrategy, computed, viewChild, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';

import { JsonEditorComponent } from '../../../../shared/components/json-editor/json-editor.component';
import { RedisTypeHandlerRegistry, HashField, SortedSetMember } from '../../../../core/value-handlers';
import { RedisDataType, RedisValue } from '../../../../models/redis-keys.types';

/**
 * Value editor data interface
 */
export interface ValueEditorInput {
  key?: string;
  type?: RedisDataType;
  value?: RedisValue;
  ttl?: number;
  connectionId?: string;
  isNew?: boolean;
}

/**
 * Value editor output data
 */
export interface ValueEditorOutput {
  key: string;
  type: RedisDataType;
  value: RedisValue;
  ttl?: number;
}

/**
 * Comprehensive Redis value editor component
 *
 * Features:
 * - Support for all Redis data types (String, List, Set, Hash, SortedSet, Stream)
 * - Visual form-based editing
 * - Raw JSON editing with syntax highlighting
 * - Type conversion and validation
 * - Add new keys or edit existing ones
 *
 * @example
 * ```typescript
 * <app-storage-value-editor
 *   [data]="editorData"
 *   [connectionId]="connectionId"
 *   (save)="onSave($event)"
 *   (cancel)="onCancel()">
 * </app-storage-value-editor>
 * ```
 */
@Component({
  selector: 'app-storage-value-editor',
  standalone: true,
  imports: [
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
    MatTooltipModule,
    MatDividerModule,
    MatExpansionModule,
    MatDialogModule,
    JsonEditorComponent
],
  // Support both standalone usage and dialog usage
  providers: [],
  templateUrl: './storage-value-editor.component.html',
  styleUrl: './storage-value-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageValueEditorComponent {
  // 1. Injected dependencies
  private readonly fb = inject(FormBuilder);
  private readonly handlerRegistry = inject(RedisTypeHandlerRegistry);
  private readonly destroyRef = inject(DestroyRef);

  // Optional: Support dialog usage
  private readonly dialogRef = inject(MatDialogRef<StorageValueEditorComponent>, { optional: true });

  // Form instance
  form!: FormGroup;
  private readonly dialogData = inject<{ data?: ValueEditorInput; connectionId?: string }>(MAT_DIALOG_DATA, { optional: true });

  // Redis type configuration
  readonly redisTypes = [
    { value: RedisDataType.String, label: 'String', icon: 'text_fields' },
    { value: RedisDataType.List, label: 'List', icon: 'list' },
    { value: RedisDataType.Set, label: 'Set', icon: 'group_work' },
    { value: RedisDataType.Hash, label: 'Hash', icon: 'table_rows' },
    { value: RedisDataType.SortedSet, label: 'Sorted Set', icon: 'sort' },
    { value: RedisDataType.Stream, label: 'Stream', icon: 'stream' }
  ] as const;

  // 2. Inputs
  readonly data = input<ValueEditorInput | null>(null);
  readonly connectionId = input<string>('');
  readonly loading = input<boolean>(false);

  // 3. Outputs
  readonly save = output<ValueEditorOutput>();
  readonly cancel = output<void>();

  // 4. View queries
  readonly jsonEditor = viewChild<JsonEditorComponent>('jsonEditor');

  // 5. State signals
  readonly selectedType = signal<RedisDataType>(RedisDataType.String);
  readonly useJsonEditor = signal<boolean>(false);
  readonly isNewKey = signal<boolean>(true);
  readonly validationErrors = signal<string[]>([]);
  readonly listItems = signal<string[]>([]);
  readonly newListItem = signal<string>('');
  readonly hashFields = signal<HashField[]>([]);
  readonly newHashKey = signal<string>('');
  readonly newHashValue = signal<string>('');
  readonly sortedSetMembers = signal<SortedSetMember[]>([]);
  readonly newSortedMember = signal<string>('');
  readonly newSortedScore = signal<number>(0);
  readonly streamEntries = signal<Array<{ id: string; fields: Array<{ key: string; value: string }> }>>([]);
  readonly jsonEditorValue = signal<string>('{}');
  readonly jsonEditorValid = signal<boolean>(true);

  // Expose RedisDataType enum to template
  readonly RedisDataType = RedisDataType;

  // 6. Computed signals
  readonly jsonRepresentation = computed(() => {
    try {
      const type = this.selectedType();
      const handler = this.handlerRegistry.getHandler(type);
      const visualValue = this.getVisualValue(type);
      return handler.toJson(visualValue);
    } catch (error) {
      console.error('Error generating JSON representation:', error);
      return '{}';
    }
  });

  readonly isValid = computed(() => {
    // Form must be valid (key and type are required)
    if (!this.form.valid) {
      return false;
    }

    // For JSON editor, check JSON validity
    if (this.useJsonEditor()) {
      return this.jsonEditorValid();
    }

    // Check validation errors
    if (this.validationErrors().length > 0) {
      return false;
    }

    // For new keys, allow empty values - validation will pass if structure is valid
    const type = this.selectedType();
    const formKey = this.form.get('key')?.value;

    // Key is required
    if (!formKey || !formKey.trim()) {
      return false;
    }

    // Type is required
    if (!type) {
      return false;
    }

    // All checks passed
    return true;
  });

  /** Computed signal for key field error message */
  readonly keyError = computed(() => {
    const control = this.form.get('key');
    if (!control || !control.touched) return null;
    if (control.hasError('required')) return 'Key is required';
    return null;
  });

  /** Computed signal for type field error message */
  readonly typeError = computed(() => {
    const control = this.form.get('type');
    if (!control || !control.touched) return null;
    if (control.hasError('required')) return 'Type is required';
    return null;
  });

  constructor() {
    this.form = this.fb.group({
      key: ['', Validators.required],
      type: [RedisDataType.String, Validators.required],
      stringValue: [''], // For String type
      ttl: [null]
    });

    // Watch for data changes (from input or dialog)
    effect(() => {
      // Check if used in dialog first
      const dialogData = this.dialogData;
      if (dialogData?.data && dialogData.connectionId) {
        this.loadData({
          ...dialogData.data,
          connectionId: dialogData.connectionId
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

    // Watch for type changes from form
    effect(() => {
      const type = this.form.get('type')?.value;
      if (type && type !== this.selectedType()) {
        this.selectedType.set(type);
        this.updateJsonEditor();
      }
    });

    // Watch for key changes and validate
    effect(() => {
      const key = this.form.get('key')?.value;
      if (key !== undefined) {
        if (this.useJsonEditor()) {
          this.validateJson();
        } else {
          this.validateValue();
        }
      }
    });

    // Watch for string value changes
    effect(() => {
      const stringValue = this.form.get('stringValue')?.value;
      if (stringValue !== undefined && !this.useJsonEditor() && this.selectedType() === RedisDataType.String) {
        this.validateValue();
      }
    });

    // Watch for visual value changes (listItems, hashFields, etc.) via effects
    effect(() => {
      const type = this.selectedType();
      const formKey = this.form.get('key')?.value;

      // Only validate if key is provided and not using JSON editor
      if (formKey && formKey.trim() && !this.useJsonEditor()) {
        // Access the visual values to trigger effect when they change
        // This effect will re-run whenever any of these signals change
        if (type === RedisDataType.List || type === RedisDataType.Set) {
          this.listItems(); // Trigger effect when listItems changes
        } else if (type === RedisDataType.Hash) {
          this.hashFields(); // Trigger effect when hashFields changes
        } else if (type === RedisDataType.SortedSet) {
          this.sortedSetMembers(); // Trigger effect when sortedSetMembers changes
        } else if (type === RedisDataType.Stream) {
          this.streamEntries(); // Trigger effect when streamEntries changes
        }

        // Validate after accessing the signals
        this.validateValue();
      }
    });

    // Watch JSON editor value changes
    effect(() => {
      if (this.useJsonEditor() && this.form.get('key')?.value) {
        this.validateJson();
      }
    });
  }

  /**
   * Load data into editor
   */
  private loadData(data: ValueEditorInput): void {
    this.isNewKey.set(data.isNew ?? false);

    const type = data.type || RedisDataType.String;
    this.selectedType.set(type);

    this.form.patchValue({
      key: data.key || '',
      type: type,
      ttl: data.ttl ?? null
    }, { emitEvent: false });

    // Load value using handler
    if (data.value) {
      const handler = this.handlerRegistry.getHandler(type);
      const visualValue = handler.toVisual(data.value);
      this.applyVisualValue(type, visualValue);
    } else {
      // Initialize with default value
      const handler = this.handlerRegistry.getHandler(type);
      const defaultValue = handler.getDefaultValue();
      this.applyVisualValue(type, defaultValue);
    }

    // Update JSON editor
    this.updateJsonEditor();
  }

  /**
   * Reset editor to initial state
   */
  private reset(): void {
    this.isNewKey.set(true);
    this.selectedType.set(RedisDataType.String);
    this.useJsonEditor.set(false);
    this.listItems.set([]);
    this.hashFields.set([]);
    this.sortedSetMembers.set([]);
    this.streamEntries.set([]);
    this.validationErrors.set([]);

    this.form.reset({
      key: '',
      type: RedisDataType.String,
      stringValue: '', // Initialize with empty string
      ttl: null
    });

    this.updateJsonEditor();
    // Clear validation errors for new key
    this.validationErrors.set([]);
  }

  /**
   * Apply visual value to appropriate signal based on type
   *
   * @param type Redis data type
   * @param visualValue Visual representation of the value (format depends on type)
   */
  private applyVisualValue(type: RedisDataType, visualValue: unknown): void {
    switch (type) {
      case RedisDataType.String:
        this.form.patchValue({ stringValue: visualValue || '' }, { emitEvent: false });
        break;
      case RedisDataType.List:
      case RedisDataType.Set:
        this.listItems.set(Array.isArray(visualValue) ? visualValue : []);
        break;
      case RedisDataType.Hash:
        this.hashFields.set(Array.isArray(visualValue) ? visualValue : []);
        break;
      case RedisDataType.SortedSet:
        this.sortedSetMembers.set(Array.isArray(visualValue) ? visualValue : []);
        break;
      case RedisDataType.Stream:
        this.streamEntries.set(Array.isArray(visualValue) ? visualValue : []);
        break;
    }
  }

  /**
   * Get current visual value based on type
   *
   * @param type Redis data type
   * @returns Visual representation of the value (format depends on type)
   */
  private getVisualValue(type: RedisDataType): unknown {
    switch (type) {
      case RedisDataType.String:
        const stringValue = this.form.get('stringValue')?.value;
        // Return empty string if null/undefined for new keys
        return stringValue !== null && stringValue !== undefined ? stringValue : '';
      case RedisDataType.List:
      case RedisDataType.Set:
        return this.listItems();
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

  /**
   * Update JSON editor with current visual value
   */
  private updateJsonEditor(): void {
    if (this.useJsonEditor()) {
      this.jsonEditorValue.set(this.jsonRepresentation());
    }
  }

  /**
   * Toggle between visual and JSON editor
   */
  toggleJsonEditor(): void {
    const newState = !this.useJsonEditor();

    if (newState) {
      // Switching TO JSON editor - populate with current data
      this.jsonEditorValue.set(this.jsonRepresentation());
    } else {
      // Switching FROM JSON editor - parse JSON back to visual editor
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

  /**
   * Handle JSON editor value changes
   */
  onJsonChange(value: string): void {
    this.jsonEditorValue.set(value);
    this.validateJson();
  }

  /**
   * Validate JSON input
   */
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

  /**
   * Format JSON in editor
   */
  formatJson(): void {
    if (this.useJsonEditor()) {
      this.jsonEditor()?.formatJson();
    }
  }

  /**
   * Validate current value
   */
  private validateValue(): boolean {
    const type = this.selectedType();
    const handler = this.handlerRegistry.getHandler(type);
    const visualValue = this.getVisualValue(type);

    // For new keys, allow empty values (Redis supports empty structures)
    // Only validate structure, not content
    if (this.isNewKey()) {
      // For String type, empty string is valid
      if (type === RedisDataType.String) {
        const stringValue = this.form.get('stringValue')?.value ?? '';
        // Empty string is valid for new keys
        this.validationErrors.set([]);
        return true;
      }
      // For other types, empty arrays/objects are valid
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

  // ========== List/Set Operations ==========

  addListItem(): void {
    const item = this.newListItem().trim();
    if (item) {
      this.listItems.update(items => [...items, item]);
      this.newListItem.set('');
      this.updateJsonEditor();
    }
  }

  removeListItem(index: number): void {
    this.listItems.update(items => items.filter((_, i) => i !== index));
    this.updateJsonEditor();
  }

  // ========== Hash Operations ==========

  addHashField(): void {
    const key = this.newHashKey().trim();
    const value = this.newHashValue().trim();
    if (key) {
      this.hashFields.update(fields => [...fields, { key, value }]);
      this.newHashKey.set('');
      this.newHashValue.set('');
      this.updateJsonEditor();
    }
  }

  removeHashField(index: number): void {
    this.hashFields.update(fields => fields.filter((_, i) => i !== index));
    this.updateJsonEditor();
  }

  updateHashField(index: number, key: string, value: string): void {
    this.hashFields.update(fields => {
      const updated = [...fields];
      updated[index] = { key, value };
      return updated;
    });
    this.updateJsonEditor();
  }

  // ========== Sorted Set Operations ==========

  addSortedSetMember(): void {
    const member = this.newSortedMember().trim();
    if (member) {
      this.sortedSetMembers.update(members => [
        ...members,
        { member, score: this.newSortedScore() }
      ]);
      this.newSortedMember.set('');
      this.newSortedScore.set(0);
      this.updateJsonEditor();
    }
  }

  removeSortedSetMember(index: number): void {
    this.sortedSetMembers.update(members => members.filter((_, i) => i !== index));
    this.updateJsonEditor();
  }

  updateSortedSetScore(index: number, score: number): void {
    this.sortedSetMembers.update(members => {
      const updated = [...members];
      const member = updated[index];
      if (member) {
        member.score = score;
      }
      return updated;
    });
    this.updateJsonEditor();
  }

  // ========== Stream Operations ==========

  addStreamEntry(): void {
    this.streamEntries.update(entries => [
      ...entries,
      { id: `*`, fields: [] }
    ]);
    this.updateJsonEditor();
  }

  removeStreamEntry(index: number): void {
    this.streamEntries.update(entries => entries.filter((_, i) => i !== index));
    this.updateJsonEditor();
  }

  // ========== Actions ==========

  onSave(): void {
    // Validate based on editor mode
    if (this.useJsonEditor()) {
      if (!this.jsonEditorValid() || !this.form.valid) {
        return;
      }
    } else {
      if (!this.validateValue() || !this.form.valid) {
        return;
      }
    }

    const formValue = this.form.getRawValue();
    const type = this.selectedType();
    const handler = this.handlerRegistry.getHandler(type);
    let visualValue: unknown;

    if (this.useJsonEditor()) {
      // Parse from JSON editor to visual format
      try {
        visualValue = handler.fromJson(this.jsonEditorValue());
        // Validate the parsed value
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
      // Get from visual editor
      visualValue = this.getVisualValue(type);
    }

    // Convert visual format to Redis API format
    let redisValue: RedisValue;
    switch (type) {
      case RedisDataType.String:
        redisValue = { value: String(visualValue) } as RedisValue;
        break;
      case RedisDataType.List:
        redisValue = { items: Array.isArray(visualValue) ? visualValue as string[] : [] } as RedisValue;
        break;
      case RedisDataType.Set:
        redisValue = { members: Array.isArray(visualValue) ? visualValue as string[] : [] } as RedisValue;
        break;
      case RedisDataType.Hash:
        const hashObj: { [key: string]: string } = {};
        if (Array.isArray(visualValue)) {
          (visualValue as HashField[]).forEach((field: HashField) => {
          hashObj[field.key] = field.value;
        });
        }
        redisValue = { fields: hashObj } as RedisValue;
        break;
      case RedisDataType.SortedSet:
        const sortedSetObj: { [member: string]: number } = {};
        if (Array.isArray(visualValue)) {
          (visualValue as SortedSetMember[]).forEach((item: SortedSetMember) => {
          sortedSetObj[item.member] = item.score;
        });
        }
        redisValue = { membersWithScores: sortedSetObj } as RedisValue;
        break;
      case RedisDataType.Stream:
        redisValue = { entries: Array.isArray(visualValue) ? visualValue as Array<{ id: string; fields: { [key: string]: string } }> : [] } as RedisValue;
        break;
      default:
        redisValue = { value: String(visualValue) } as RedisValue;
    }

    const output: ValueEditorOutput = {
      key: formValue.key,
      type: type,
      value: redisValue,
      ttl: formValue.ttl
    };

    // If used in dialog, close with result
    if (this.dialogRef) {
      this.dialogRef.close(output);
    } else {
      // Emit save event for standalone usage
      this.save.emit(output);
    }
  }

  onCancel(): void {
    // If used in dialog, close without result
    if (this.dialogRef) {
      this.dialogRef.close();
    } else {
      // Emit cancel event for standalone usage
      this.cancel.emit();
    }
  }

  /**
   * Get type icon
   */
  getTypeIcon(type: RedisDataType): string {
    const typeInfo = this.redisTypes.find(t => t.value === type);
    return typeInfo?.icon || 'description';
  }
}
