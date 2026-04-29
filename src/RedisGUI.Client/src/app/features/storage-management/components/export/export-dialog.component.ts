import { Component, inject, signal, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { FormControl, ReactiveFormsModule } from '@angular/forms';

import { MatButtonModule } from '@angular/material/button';
import { MatRadioModule } from '@angular/material/radio';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { ExportService } from '../../services/export.service';

/**
 * Export Dialog Component
 * Dialog for exporting Redis data to file
 * 
 * Features:
 * - Format selection (JSON/CSV)
 * - Export progress indicator
 * - Error handling
 * - Automatic file download
 * 
 * @example
 * ```typescript
 * this.dialog.open(ExportDialogComponent, {
 *   data: { connectionId: 'conn-123' }
 * });
 * ```
 */
@Component({
  selector: 'app-export-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatRadioModule,
    MatProgressSpinnerModule,
    ReactiveFormsModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions
  ],
  templateUrl: './export-dialog.component.html',
  styleUrl: './export-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExportDialogComponent {
  // 1. Injected dependencies
  private readonly exportService = inject(ExportService);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ExportDialogComponent>);
  private readonly data = inject<{ connectionId: string }>(MAT_DIALOG_DATA);

  // 2. Form controls
  /** Export format selection */
  readonly formatCtrl = new FormControl<'json' | 'csv'>('json', { nonNullable: true });

  // 3. State signals
  /** Loading state during export */
  readonly isLoading = signal<boolean>(false);
  
  /** Error message if export fails */
  readonly error = signal<string | null>(null);

  /**
   * Handles export action
   * Downloads Redis data in the selected format (JSON or CSV)
   */
  onExport(): void {
    this.error.set(null);
    this.isLoading.set(true);

    const format = this.formatCtrl.value;
    
    this.exportService.downloadExport(this.data.connectionId, format)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (err: Error) => {
          this.error.set(err.message || 'Export failed. Please try again.');
        }
      });
  }
}

