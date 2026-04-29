import { Component, inject, input, output, signal, effect, computed, ChangeDetectionStrategy, DestroyRef } from '@angular/core';
import { UpperCasePipe, KeyValuePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule, MatDialog } from '@angular/material/dialog';
import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';
import { API_ROUTES } from '../../../core/constants/api-routes.constants';
import { ConfirmDialogComponent } from '../../../shared/components';

import { RedisValue, HashValueData, ListValueData, SetValueData, SortedSetValueData, StreamValueData } from '../../../models/redis-keys.types';

// Internal representation for editing - uses plain types for easier manipulation
type InternalValue =
  | string
  | Record<string, string>
  | string[]
  | Array<{ member: string; score: number }>
  | Array<{ id: string; fields: Record<string, string> }>;

interface KeyValueDetails {
  key: string;
  type: string;
  ttl: number;
  size: number;
  value: InternalValue;
}

@Component({
  selector: 'app-key-editor',
  standalone: true,
  templateUrl: './key-editor.component.html',
  styleUrl: './key-editor.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    UpperCasePipe,
    KeyValuePipe,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDialogModule
  ]
})
export class KeyEditorComponent {
  // 1. Injected dependencies
  private readonly http = inject(HttpClient);
  private readonly dialog = inject(MatDialog);;
  private readonly destroyRef = inject(DestroyRef);

  // Optional dialog support (for dialog usage)
  private readonly dialogRef = inject(MatDialogRef<KeyEditorComponent>, { optional: true });
  private readonly dialogData = inject<{ connectionId?: string; key?: string }>(MAT_DIALOG_DATA, { optional: true });

  // 2. Inputs (for standalone usage)
  /** Redis connection ID */
  readonly connectionId = input<string>('');
  
  /** Redis key to edit */
  readonly key = input<string>('');

  // 3. Outputs (for standalone usage)
  /** Emitted when editor is closed */
  readonly closed = output<void>();
  
  /** Emitted when key is successfully saved */
  readonly saved = output<void>();

  // 4. Computed signals
  /** Effective connection ID (prefers dialog data over input) */
  readonly effectiveConnectionId = computed(() => this.dialogData?.connectionId || this.connectionId());
  
  /** Effective key (prefers dialog data over input) */
  readonly effectiveKey = computed(() => this.dialogData?.key || this.key());

  // 5. State signals
  /** Loading state while fetching key value */
  readonly loading = signal<boolean>(false);
  
  /** Saving state during update operation */
  readonly saving = signal<boolean>(false);
  
  /** Error message */
  readonly error = signal<string | null>(null);
  
  /** Key details (type, value, TTL, size) */
  readonly details = signal<KeyValueDetails | null>(null);
  
  /** Currently active hash field for editing */
  readonly activeHashField = signal<string>('');
  
  /** New hash field name */
  readonly newHashField = signal<string>('');
  
  /** New hash field value */
  readonly newHashValue = signal<string>('');
  
  /** New list item value */
  readonly newListItem = signal<string>('');
  
  /** New set member value */
  readonly newSetMember = signal<string>('');
  
  /** New sorted set member value */
  readonly newZSetMember = signal<string>('');
  
  /** New sorted set score */
  readonly newZSetScore = signal<number>(0);
  
  /** Stream entry fields */
  readonly streamNewFields = signal<{ field: string; value: string }[]>([{ field: '', value: '' }]);

  // 6. Private state
  /** Last loaded key to prevent duplicate loads */
  private readonly lastLoadedKey = signal<string | null>(null);

  constructor() {
    // Load key when connectionId or key changes
    effect(() => {
      const connId = this.effectiveConnectionId();
      const keyValue = this.effectiveKey();
      const lastKey = this.lastLoadedKey();

      if (connId && keyValue && keyValue !== lastKey) {
        this.lastLoadedKey.set(keyValue);
        this.loadKey(connId, keyValue);
      }
    });
  }

  private loadKey(connectionId: string, key: string): void {
    this.loading.set(true);
    this.error.set(null);
    interface GetValueResponse {
      key: string;
      type: string;
      ttl: number | null;
      size: number;
      value: unknown;
    }

    this.http.get<GetValueResponse>(API_ROUTES.STORAGE.GET_VALUE(connectionId, key))
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          let parsed: unknown = res.value;
          if (res.type === 'string' && typeof res.value === 'string') {
            try { parsed = JSON.parse(res.value); } catch { parsed = res.value; }
          }
          this.details.set({
            key: res.key,
            type: res.type,
            ttl: res.ttl ?? 0,
            size: res.size,
            value: this.normalizeValue(res.type, parsed)
          });
        },
        error: err => this.error.set(err.message || 'Failed to load key'),
        complete: () => this.loading.set(false)
      });
  }

  // 7. TrackBy functions (for @for loops performance)
  /** TrackBy function for array indices */
  readonly trackByIndex = (index: number) => index;
  
  /** TrackBy function for objects with key property */
  readonly trackByKey = (index: number, item: { key: string }) => item.key;
  
  /** TrackBy function for string members */
  readonly trackByMember = (index: number, item: string) => item;
  
  /** TrackBy function for stream entries */
  readonly trackByEntryId = (index: number, item: { id: string }) => item.id;
  
  /** TrackBy function for stream fields */
  readonly trackByStreamField = (index: number, item: { field: string; value: string }) => `${item.field}-${index}`;

  // Helper method for template to convert to string
  toString(value: unknown): string {
    return String(value);
  }

  // Helper methods for template to access typed values
  getListItems(): string[] {
    const d = this.details();
    if (!d || !this.isList(d.value)) return [];
    return d.value;
  }

  getSetMembers(): string[] {
    const d = this.details();
    if (!d || !this.isSet(d.value)) return [];
    return d.value;
  }

  getZSetItems(): Array<{ member: string; score: number }> {
    const d = this.details();
    if (!d || !this.isZSet(d.value)) return [];
    return d.value;
  }

  getStreamEntries(): Array<{ id: string; fields: Record<string, string> }> {
    const d = this.details();
    if (!d || !this.isStream(d.value)) return [];
    return d.value;
  }

  getHashValue(): Record<string, string> {
    const d = this.details();
    if (!d || !this.isHash(d.value)) return {};
    return d.value;
  }

  getStringValue(): string {
    const d = this.details();
    if (!d || !this.isString(d.value)) return '';
    return d.value;
  }

  private getConnectionId(): string {
    return this.effectiveConnectionId() || '';
  }

  load(): void {
    const connId = this.effectiveConnectionId();
    const keyValue = this.effectiveKey();
    if (connId && keyValue) {
      this.lastLoadedKey.set(null); // Reset to force reload
      this.loadKey(connId, keyValue);
    }
  }

  close(): void {
    if (this.dialogRef) {
      this.dialogRef.close();
    } else {
      this.closed.emit();
      this.details.set(null);
    }
  }

  /**
   * Normalize Redis value based on type
   * Converts raw Redis response to expected format for each data type
   *
   * @param type Redis data type string
   * @param raw Raw value from Redis
   * @returns Normalized value in appropriate format
   */
  normalizeValue(type: string, raw: unknown): InternalValue {
    switch (type) {
      case 'hash':
        if (typeof raw === 'object' && raw !== null && !Array.isArray(raw)) {
          return raw as Record<string, string>;
        }
        return {}; // object of field->value
      case 'list':
        return Array.isArray(raw) ? raw as string[] : []; // list items
      case 'set':
        return Array.isArray(raw) ? raw as string[] : []; // set members
      case 'zset':
        return Array.isArray(raw) ? raw as Array<{ member: string; score: number }> : []; // [{member,score}]
      case 'stream':
        return Array.isArray(raw) ? raw as Array<{ id: string; fields: Record<string, string> }> : []; // [{id, fields:{}}]
      default:
        return typeof raw === 'string' ? raw : String(raw ?? '');
    }
  }

  // Type guards for internal value
  isString(value: InternalValue): value is string {
    return typeof value === 'string';
  }

  isHash(value: InternalValue): value is Record<string, string> {
    return typeof value === 'object' && value !== null && !Array.isArray(value);
  }

  isList(value: InternalValue): value is string[] {
    return Array.isArray(value) && this.details()?.type === 'list';
  }

  isSet(value: InternalValue): value is string[] {
    return Array.isArray(value) && this.details()?.type === 'set';
  }

  isZSet(value: InternalValue): value is Array<{ member: string; score: number }> {
    return Array.isArray(value) && this.details()?.type === 'zset';
  }

  isStream(value: InternalValue): value is Array<{ id: string; fields: Record<string, string> }> {
    return Array.isArray(value) && this.details()?.type === 'stream';
  }

  // ========== STRING ==========
  updateString(newValue: string): void {
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: 0, // String enum value
      value: newValue,
      ttl: d.ttl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => {
        d.value = newValue as InternalValue;
        this.saved.emit();
        if (this.dialogRef) {
          this.dialogRef.close(true);
        }
      },
      error: err => this.error.set(err.message || 'Save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== HASH ==========
  addHashField(): void {
    const d = this.details(); if (!d || d.type !== 'hash') return;
    if (!this.isHash(d.value)) return;
    const field = this.newHashField().trim();
    if (!field) return;
    d.value[field] = this.newHashValue();
    this.newHashField.set(''); this.newHashValue.set('');
    this.persistHash(d.value);
  }
  /**
   * Update a hash field value
   */
  updateHashField(field: string | number, value: string | number): void {
    const d = this.details();
    if (!d || !this.isHash(d.value)) return;
    d.value[String(field)] = String(value);
    this.persistHash(d.value);
  }

  /**
   * Delete a hash field
   */
  deleteHashField(field: string | number): void {
    const d = this.details();
    if (!d || !this.isHash(d.value)) return;
    delete d.value[String(field)];
    this.persistHash(d.value);
  }
  /**
   * Persist hash object to Redis
   */
  persistHash(hashObj: Record<string, string>): void {
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: 3, // Hash enum value
      value: JSON.stringify(hashObj),
      ttl: d.ttl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => {
        this.saved.emit();
      },
      error: err => this.error.set(err.message || 'Hash save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== LIST ==========
  pushList(direction: 'left' | 'right'): void {
    const item = this.newListItem().trim(); if (!item) return;
    const d = this.details(); if (!d || d.type !== 'list' || !this.isList(d.value)) return;
    if (direction === 'left') d.value.unshift(item); else d.value.push(item);
    this.newListItem.set('');
    this.persistList(d.value);
  }
  popList(direction: 'left' | 'right'): void {
    const d = this.details(); if (!d || d.type !== 'list' || !this.isList(d.value)) return;
    if (d.value.length === 0) return;
    if (direction === 'left') d.value.shift(); else d.value.pop();
    this.persistList(d.value);
  }
  persistList(list: string[]): void {
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: 1, // List enum value
      value: JSON.stringify(list),
      ttl: d.ttl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => {
        this.saved.emit();
      },
      error: err => this.error.set(err.message || 'List save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== SET ==========
  addSetMember(): void {
    const member = this.newSetMember().trim(); if (!member) return;
    const d = this.details(); if (!d || d.type !== 'set' || !this.isSet(d.value)) return;
    if (!d.value.includes(member)) d.value.push(member);
    this.newSetMember.set('');
    this.persistSet(d.value);
  }
  removeSetMember(member: string): void {
    const d = this.details(); if (!d || d.type !== 'set' || !this.isSet(d.value)) return;
    d.value = d.value.filter((m: string) => m !== member);
    this.persistSet(d.value);
  }
  persistSet(setMembers: string[]): void {
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: 2, // Set enum value
      value: JSON.stringify(setMembers),
      ttl: d.ttl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => {
        this.saved.emit();
      },
      error: err => this.error.set(err.message || 'Set save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== ZSET ==========
  addZSetMember(): void {
    const member = this.newZSetMember().trim(); if (!member) return;
    const score = this.newZSetScore();
    const d = this.details(); if (!d || d.type !== 'zset' || !this.isZSet(d.value)) return;
    const existing = d.value.find((x: { member: string; score: number }) => x.member === member);
    if (existing) existing.score = score; else d.value.push({ member, score });
    this.newZSetMember.set(''); this.newZSetScore.set(0);
    this.persistZSet(d.value);
  }
  updateZSetScore(member: string, score: number): void {
    const d = this.details(); if (!d || d.type !== 'zset' || !this.isZSet(d.value)) return;
    const existing = d.value.find((x: { member: string; score: number }) => x.member === member);
    if (existing) existing.score = score;
    this.persistZSet(d.value);
  }
  removeZSetMember(member: string): void {
    const d = this.details(); if (!d || d.type !== 'zset' || !this.isZSet(d.value)) return;
    d.value = d.value.filter((x: { member: string; score: number }) => x.member !== member);
    this.persistZSet(d.value);
  }
  /**
   * Persist sorted set (zset) value
   *
   * @param zset Array of member-score pairs
   */
  persistZSet(zset: Array<{ member: string; score: number }>): void {
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: 4, // SortedSet enum value
      value: JSON.stringify(zset),
      ttl: d.ttl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => {
        this.saved.emit();
      },
      error: err => this.error.set(err.message || 'ZSet save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== STREAM ==========
  addStreamField(): void {
    this.streamNewFields.update(list => [...list, { field: '', value: '' }]);
  }
  removeStreamField(index: number): void {
    this.streamNewFields.update(list => list.filter((_, i) => i !== index));
  }
  createStreamEntry(): void {
    const d = this.details(); if (!d || d.type !== 'stream' || !this.isStream(d.value)) return;
    const fieldsObj: Record<string, string> = {};
    for (const f of this.streamNewFields()) {
      if (f.field.trim()) fieldsObj[f.field.trim()] = f.value;
    }
    const entry = { id: '*', fields: fieldsObj };
    d.value.push(entry);
    this.streamNewFields.set([{ field: '', value: '' }]);
    this.persistStream(d.value);
  }
  /**
   * Persist stream entries
   *
   * @param entries Array of stream entries with id and fields (fields as Record)
   */
  persistStream(entries: Array<{ id: string; fields: Record<string, string> }>): void {
    // Convert to the format expected by the API
    const convertedEntries: Array<{ id: string; fields: Array<{ key: string; value: string }> }> = entries.map(entry => ({
      id: entry.id,
      fields: Object.entries(entry.fields).map(([key, value]) => ({ key, value }))
    }));
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: 5, // Stream enum value
      value: JSON.stringify(convertedEntries),
      ttl: d.ttl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => {
        this.saved.emit();
      },
      error: err => this.error.set(err.message || 'Stream save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== TTL ==========
  updateTTL(newTtl: number): void {
    const d = this.details(); if (!d) return;
    const connectionId = this.getConnectionId();
    const typeMap: Record<string, number> = {
      'string': 0, 'list': 1, 'set': 2, 'hash': 3, 'zset': 4, 'stream': 5
    };
    this.saving.set(true);
    this.http.put(API_ROUTES.STORAGE.UPDATE_VALUE(connectionId, d.key), {
      type: typeMap[d.type] ?? 0,
      value: JSON.stringify(d.value),
      ttl: newTtl
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
      next: () => d.ttl = newTtl,
      error: err => this.error.set(err.message || 'TTL update failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== Delete ==========
  deleteKey(): void {
    const d = this.details();
    if (!d) return;

    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Key',
        message: `Are you sure you want to delete the key "${d.key}"?`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        confirmColor: 'warn'
      }
    }).afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        const connectionId = this.getConnectionId();
        this.loading.set(true);
        this.http.delete(API_ROUTES.STORAGE.DELETE_VALUE(connectionId, d.key))
          .pipe(takeUntilDestroyed(this.destroyRef))
          .subscribe({
            next: () => {
              this.details.set(null);
              if (this.dialogRef) {
                this.dialogRef.close(true);
              }
            },
            error: err => this.error.set(err.message || 'Delete failed'),
            complete: () => this.loading.set(false)
          });
      });
  }
}
