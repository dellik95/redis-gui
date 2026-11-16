import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { firstValueFrom } from 'rxjs';
import { BulkTtlResult } from '../../../../core/stores/redis-keys.store';
import { DeleteMultipleKeysResponse } from '../../../../models/redis-advanced.types';

interface BulkSelectedData {
  connectionId: string;
  database: number;
  keys: string[];
  operation: 'delete' | 'ttl';
  ttlSeconds?: number | null;
}

@Component({
  selector: 'app-bulk-selected-progress',
  standalone: true,
  imports: [MatDialogModule, MatButtonModule, MatProgressBarModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <h2 mat-dialog-title>{{ data.operation === 'delete' ? 'Delete selected keys' : 'Update selected TTLs' }}</h2>
    <mat-dialog-content>
      <p>Database {{ data.database }} · {{ processed() }} of {{ data.keys.length }} keys processed</p>
      <mat-progress-bar mode="determinate" [value]="processed() * 100 / data.keys.length" />
      <p>Changed: {{ changed() }} · Unchanged or missing: {{ unchanged() }} · Failed: {{ failed() }}</p>
      @if (message()) { <p role="status">{{ message() }}</p> }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      @if (running()) {
        <button mat-button (click)="stop()" [disabled]="stopping()">{{ stopping() ? 'Stopping after current batch…' : 'Stop' }}</button>
      } @else {
        <button mat-button (click)="close()">Close</button>
      }
    </mat-dialog-actions>
  `
})
export class BulkSelectedProgressComponent implements OnInit {
  readonly data: BulkSelectedData = inject(MAT_DIALOG_DATA);
  private readonly ref = inject(MatDialogRef<BulkSelectedProgressComponent>);
  private readonly http = inject(HttpClient);
  readonly running = signal(true);
  readonly stopping = signal(false);
  readonly processed = signal(0);
  readonly changed = signal(0);
  readonly unchanged = signal(0);
  readonly failed = signal(0);
  readonly message = signal('');

  ngOnInit(): void { void this.run(); }
  stop(): void { this.stopping.set(true); }
  close(): void { this.ref.close(); }

  private async run(): Promise<void> {
    try {
      for (let offset = 0; offset < this.data.keys.length && !this.stopping(); offset += 100) {
        const keys = this.data.keys.slice(offset, offset + 100);
        if (this.data.operation === 'delete') {
          const result = await firstValueFrom(this.http.post<DeleteMultipleKeysResponse>(
            `/api/v1/storage/${this.data.connectionId}/keys/delete-multiple`, { keys, database: this.data.database }
          ));
          this.changed.update(value => value + result.deletedCount);
          this.failed.update(value => value + result.failedKeys.length);
        } else {
          const result = await firstValueFrom(this.http.post<BulkTtlResult>(
            `/api/v1/storage/${this.data.connectionId}/keys/bulk-ttl`,
            { keys, database: this.data.database, ttlSeconds: this.data.ttlSeconds ?? null }
          ));
          this.changed.update(value => value + result.updatedCount);
          this.unchanged.update(value => value + result.unchangedCount);
          this.failed.update(value => value + result.failedKeys.length);
        }
        this.processed.update(value => value + keys.length);
      }
      this.message.set(this.stopping() && this.processed() < this.data.keys.length
        ? 'Stopped after the current batch. Remaining keys were not processed.' : 'Operation completed.');
    } catch {
      this.message.set('A batch failed or its result was lost. Some keys in that batch may have changed; refresh before retrying.');
    } finally {
      this.running.set(false);
    }
  }
}
