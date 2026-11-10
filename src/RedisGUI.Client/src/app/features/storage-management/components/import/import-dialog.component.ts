import { Component, inject, signal, computed, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';

import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { ImportService } from '../../services/import.service';

/**
 * Import Dialog Component
 * Dialog for importing Redis data from file
 *
 * Features:
 * - File selection (JSON/CSV)
 * - Import progress indicator
 * - Error handling
 * - Automatic validation
 *
 * @example
 * ```typescript
 * this.dialog.open(ImportDialogComponent, {
 *   data: { connectionId: 'conn-123' }
 * });
 * ```
 */
@Component({
  selector: 'app-import-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatProgressSpinnerModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions
  ],
  templateUrl: './import-dialog.component.html',
  styleUrl: './import-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImportDialogComponent {
  // 1. Injected dependencies
  private readonly importService = inject(ImportService);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ImportDialogComponent>);
  private readonly data = inject<{ connectionId: string }>(MAT_DIALOG_DATA);

  // 2. State signals
  /** Selected file for import */
  readonly file = signal<File | null>(null);

  /** Loading state during import */
  readonly isLoading = signal<boolean>(false);

  /** Error message if import fails */
  readonly error = signal<string | null>(null);

  // 3. Computed signals
  /** Whether import can be executed */
  readonly canImport = computed(() => {
    return this.file() !== null && !this.isLoading();
  });

  /**
   * Handle file selection
   * @param event - File input change event
   */
  onFileChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input?.files && input.files.length > 0) {
      const selectedFile = input.files[0];
      this.file.set(selectedFile ?? null);
      this.error.set(null);
    }
  }

  /**
   * Handle import action
   * Imports the selected file to Redis
   */
  onImport(): void {
    const fileValue = this.file();
    if (!fileValue) return;

    this.error.set(null);
    this.isLoading.set(true);

    this.importService.importData(this.data.connectionId, fileValue)
      .pipe(
        finalize(() => this.isLoading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.dialogRef.close(true);
        },
        error: (err: Error) => {
          this.error.set(err.message || 'Import failed. Ensure file format is correct.');
        }
      });
  }
}

