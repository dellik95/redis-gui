import { Component, inject, input, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RedisOperationsService } from '../../../services/api/redis-operations.service';
import { HttpClient } from '@angular/common/http';

interface KeyValueDetails {
  key: string;
  type: string;
  ttl: number;
  size: number;
  value: any;
}

@Component({
  selector: 'app-key-editor',
  standalone: true,
  templateUrl: './key-editor.component.html',
  styleUrls: ['./key-editor.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule
  ]
})
export class KeyEditorComponent {
  connectionId = input.required<string>();
  key = input.required<string>();

  private http = inject(HttpClient);

  loading = signal<boolean>(false);
  saving = signal<boolean>(false);
  error = signal<string | null>(null);
  details = signal<KeyValueDetails | null>(null);
  activeHashField = signal<string>('');
  newHashField = signal<string>('');
  newHashValue = signal<string>('');
  newListItem = signal<string>('');
  newSetMember = signal<string>('');
  newZSetMember = signal<string>('');
  newZSetScore = signal<number>(0);
  streamNewFields = signal<{ field: string; value: string }[]>([{ field: '', value: '' }]);

  constructor() {
    effect(() => {
      if (this.connectionId() && this.key()) {
        this.load();
      }
    });
  }

  // Used for *ngFor trackBy in template
  trackByKey(index: number, item: any): string {
    return String(item.key);
  }

  private apiBase(): string {
    return `/api/v1/redis/${this.connectionId()}`;
  }

  load(): void {
    this.loading.set(true);
    this.error.set(null);
    this.http.get<any>(`${this.apiBase()}/values/${encodeURIComponent(this.key())}`).subscribe({
      next: res => {
        // Attempt to parse value JSON if possible
        let parsed: any = res.value;
        if (res.type === 'string') {
          try { parsed = JSON.parse(res.value); } catch { parsed = res.value; }
        }
        this.details.set({
          key: res.key,
          type: res.type,
          ttl: res.ttl,
          size: res.size,
          value: this.normalizeValue(res.type, parsed)
        });
      },
      error: err => this.error.set(err.message || 'Failed to load key'),
      complete: () => this.loading.set(false)
    });
  }

  normalizeValue(type: string, raw: any): any {
    switch (type) {
      case 'hash': return raw || {}; // object of field->value
      case 'list': return Array.isArray(raw) ? raw : []; // list items
      case 'set': return Array.isArray(raw) ? raw : []; // set members
      case 'zset': return Array.isArray(raw) ? raw : []; // [{member,score}]
      case 'stream': return Array.isArray(raw) ? raw : []; // [{id, fields:{}}]
      default: return raw;
    }
  }

  // ========== STRING ==========
  updateString(newValue: string): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: newValue, type: 'string' }).subscribe({
      next: () => d.value = newValue,
      error: err => this.error.set(err.message || 'Save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== HASH ==========
  addHashField(): void {
    const d = this.details(); if (!d || d.type !== 'hash') return;
    const field = this.newHashField().trim();
    if (!field) return;
    d.value[field] = this.newHashValue();
    this.newHashField.set(''); this.newHashValue.set('');
    this.persistHash(d.value);
  }
  updateHashField(field: unknown, value: unknown): void {
    const d = this.details(); if (!d) return;
    d.value[String(field)] = String(value);
    this.persistHash(d.value);
  }
  deleteHashField(field: unknown): void {
    const d = this.details(); if (!d) return;
    delete d.value[String(field)];
    this.persistHash(d.value);
  }
  persistHash(hashObj: any): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: JSON.stringify(hashObj), type: 'hash' }).subscribe({
      error: err => this.error.set(err.message || 'Hash save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== LIST ==========
  pushList(direction: 'left' | 'right'): void {
    const item = this.newListItem().trim(); if (!item) return;
    const d = this.details(); if (!d || d.type !== 'list') return;
    if (direction === 'left') d.value.unshift(item); else d.value.push(item);
    this.newListItem.set('');
    this.persistList(d.value);
  }
  popList(direction: 'left' | 'right'): void {
    const d = this.details(); if (!d || d.type !== 'list') return;
    if (d.value.length === 0) return;
    if (direction === 'left') d.value.shift(); else d.value.pop();
    this.persistList(d.value);
  }
  persistList(list: string[]): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: JSON.stringify(list), type: 'list' }).subscribe({
      error: err => this.error.set(err.message || 'List save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== SET ==========
  addSetMember(): void {
    const member = this.newSetMember().trim(); if (!member) return;
    const d = this.details(); if (!d || d.type !== 'set') return;
    if (!d.value.includes(member)) d.value.push(member);
    this.newSetMember.set('');
    this.persistSet(d.value);
  }
  removeSetMember(member: string): void {
    const d = this.details(); if (!d || d.type !== 'set') return;
    d.value = d.value.filter((m: string) => m !== member);
    this.persistSet(d.value);
  }
  persistSet(setMembers: string[]): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: JSON.stringify(setMembers), type: 'set' }).subscribe({
      error: err => this.error.set(err.message || 'Set save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== ZSET ==========
  addZSetMember(): void {
    const member = this.newZSetMember().trim(); if (!member) return;
    const score = this.newZSetScore();
    const d = this.details(); if (!d || d.type !== 'zset') return;
    const existing = d.value.find((x: any) => x.member === member);
    if (existing) existing.score = score; else d.value.push({ member, score });
    this.newZSetMember.set(''); this.newZSetScore.set(0);
    this.persistZSet(d.value);
  }
  updateZSetScore(member: string, score: number): void {
    const d = this.details(); if (!d || d.type !== 'zset') return;
    const existing = d.value.find((x: any) => x.member === member); if (existing) existing.score = score;
    this.persistZSet(d.value);
  }
  removeZSetMember(member: string): void {
    const d = this.details(); if (!d || d.type !== 'zset') return;
    d.value = d.value.filter((x: any) => x.member !== member);
    this.persistZSet(d.value);
  }
  persistZSet(zset: any[]): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: JSON.stringify(zset), type: 'zset' }).subscribe({
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
    const d = this.details(); if (!d || d.type !== 'stream') return;
    const fieldsObj: any = {};
    for (const f of this.streamNewFields()) {
      if (f.field.trim()) fieldsObj[f.field.trim()] = f.value;
    }
    const entry = { id: '*', fields: fieldsObj };
    d.value.push(entry);
    this.streamNewFields.set([{ field: '', value: '' }]);
    this.persistStream(d.value);
  }
  persistStream(entries: any[]): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: JSON.stringify(entries), type: 'stream' }).subscribe({
      error: err => this.error.set(err.message || 'Stream save failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== TTL ==========
  updateTTL(newTtl: number): void {
    const d = this.details(); if (!d) return;
    this.saving.set(true);
    this.http.post(`${this.apiBase()}/values`, { key: d.key, value: JSON.stringify(d.value), type: d.type, ttl: newTtl }).subscribe({
      next: () => d.ttl = newTtl,
      error: err => this.error.set(err.message || 'TTL update failed'),
      complete: () => this.saving.set(false)
    });
  }

  // ========== Delete ==========
  deleteKey(): void {
    if (!confirm('Delete this key?')) return;
    const d = this.details(); if (!d) return;
    this.loading.set(true);
    this.http.delete(`${this.apiBase()}/values/${encodeURIComponent(d.key)}`).subscribe({
      next: () => this.details.set(null),
      error: err => this.error.set(err.message || 'Delete failed'),
      complete: () => this.loading.set(false)
    });
  }
}
