import { Injectable, inject } from '@angular/core';
import { Observable, from, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { BackgroundJob, BackgroundJobsService } from '../../../core/services/background-jobs.service';

export interface ImportResult {
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  failedKeys: string[];
  cancelled: boolean;
  mayHaveUnconfirmedChanges: boolean;
}

/**
 * Import Service
 * Handles Redis data import operations
 *
 * Provides a clean interface for importing Redis data from files (JSON/CSV)
 *
 * @example
 * ```typescript
 * private importService = inject(ImportService);
 *
 * // Import data
 * this.importService.importData('conn-123', file).subscribe({
 *   next: () => console.log('Import successful'),
 *   error: (err) => console.error('Import failed:', err)
 * });
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class ImportService {
  // 1. Injected dependencies
  private readonly jobs = inject(BackgroundJobsService);

  /**
   * Import Redis data from file
   * @param connectionId - The connection ID
   * @param file - File to import (JSON or CSV format)
   * @returns Observable that completes when import is done
   */
  importData(connectionId: string, file: File, database: number): Observable<BackgroundJob> {
    if (file.size > 10 * 1024 * 1024) {
      return throwError(() => new Error('Import files must not exceed 10 MB.'));
    }
    const format = file.name.toLowerCase().endsWith('.csv') ? 'csv' : 'json';
    return from(file.text()).pipe(
      switchMap(data => this.jobs.start('redis-import', connectionId, {
        data, format, overwrite: false, database
      })),
      catchError(error => {
        return throwError(
          () => new Error(error?.error?.detail || error?.error?.message || 'Import failed. Ensure file format is correct.')
        );
      })
    );
  }
}
