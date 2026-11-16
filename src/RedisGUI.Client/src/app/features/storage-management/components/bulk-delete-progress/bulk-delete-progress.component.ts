import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { TokenStorageService } from '../../../../core/services/auth/token-storage.service';

interface BulkDeleteDialogData { connectionId: string; pattern: string; database: number; }
interface BulkDeleteResult { deletedCount: number; cancelled: boolean; error: string | null; }

@Component({
  selector: 'app-bulk-delete-progress',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>Delete matching keys</h2>
    <mat-dialog-content>
      <p>Pattern: {{ data.pattern }} · Database: {{ data.database }}</p>
      @if (running()) { <mat-progress-bar mode="indeterminate" /> }
      <p>Confirmed deleted: {{ deleted() }}</p>
      @if (running()) { <p>The final count depends on changes to the keyspace while scanning.</p> }
      @if (message()) { <p role="status">{{ message() }}</p> }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @if (running()) {
        <button mat-button (click)="stop()" [disabled]="stopping()">{{ stopping() ? 'Stopping…' : 'Stop' }}</button>
      } @else {
        <button mat-button (click)="close()">Close</button>
      }
    </mat-dialog-actions>
  `
})
export class BulkDeleteProgressComponent implements OnInit {
  readonly data: BulkDeleteDialogData = inject(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<BulkDeleteProgressComponent>);
  private readonly http = inject(HttpClient);
  private readonly tokens = inject(TokenStorageService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly controller = new AbortController();
  private readonly operationId = this.createId();
  readonly running = signal(true);
  readonly stopping = signal(false);
  readonly deleted = signal(0);
  readonly message = signal('');

  constructor() { this.destroyRef.onDestroy(() => this.controller.abort()); }

  ngOnInit(): void { void this.run(); }

  stop(): void {
    this.stopping.set(true);
    this.http.post(`/api/v1/storage/${this.data.connectionId}/keys/pattern-delete/stop`, { operationId: this.operationId })
      .subscribe({ error: () => { this.stopping.set(false); this.message.set('Could not stop the operation. Try again or wait for its result.'); } });
  }

  close(): void { this.ref.close(); }

  private async run(): Promise<void> {
    let completed = false;
    try {
      const token = this.tokens.token();
      const response = await fetch(`/api/v1/storage/${this.data.connectionId}/keys/pattern-delete/stream`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', ...(token && !this.tokens.isTokenExpired() ? { Authorization: `Bearer ${token}` } : {}) },
        body: JSON.stringify({ operationId: this.operationId, pattern: this.data.pattern, database: this.data.database }),
        signal: this.controller.signal
      });
      if (!response.ok) {
        const problem = await response.json().catch(() => ({})) as { detail?: string; message?: string };
        throw new Error(problem.detail || problem.message || `Request failed (${response.status}).`);
      }
      const reader = response.body?.getReader();
      if (!reader) throw new Error('Streaming is unavailable in this browser.');
      const decoder = new TextDecoder();
      let buffer = '';
      while (true) {
        const chunk = await reader.read();
        if (chunk.done) break;
        buffer += decoder.decode(chunk.value, { stream: true });
        const events = buffer.split('\n\n');
        buffer = events.pop() ?? '';
        for (const event of events) {
          const type = event.split('\n').find(line => line.startsWith('event: '))?.slice(7);
          const json = event.split('\n').find(line => line.startsWith('data: '))?.slice(6);
          if (!json) continue;
          const result = JSON.parse(json) as BulkDeleteResult;
          if (type === 'progress') this.deleted.set(result.deletedCount);
          if (type === 'complete') {
            completed = true;
            this.deleted.set(result.deletedCount);
            this.message.set(result.error || (result.cancelled ? 'Stopped. No further keys will be deleted.' : 'Deletion completed.'));
          }
        }
      }
      if (!completed) throw new Error('The connection ended before the final result. More keys may have been deleted; refresh before retrying.');
    } catch (error) {
      if (!this.controller.signal.aborted) this.message.set(error instanceof Error ? error.message : 'Deletion failed. Refresh before retrying.');
    } finally {
      this.running.set(false);
      this.stopping.set(false);
    }
  }

  private createId(): string {
    const bytes = crypto.getRandomValues(new Uint8Array(16));
    bytes[6] = (bytes[6]! & 15) | 64;
    bytes[8] = (bytes[8]! & 63) | 128;
    const hex = [...bytes].map(value => value.toString(16).padStart(2, '0')).join('');
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
  }
}
