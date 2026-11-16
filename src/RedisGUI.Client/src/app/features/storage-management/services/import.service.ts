import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, from, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

export interface ImportResult {
  importedCount: number;
  skippedCount: number;
  failedCount: number;
  failedKeys: string[];
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
  private readonly http = inject(HttpClient);

  // 2. Constants
  private readonly BASE_URL = '/api/v1/storage';

  /**
   * Import Redis data from file
   * @param connectionId - The connection ID
   * @param file - File to import (JSON or CSV format)
   * @returns Observable that completes when import is done
   */
  importData(connectionId: string, file: File, database: number): Observable<ImportResult> {
    if (file.size > 10 * 1024 * 1024) {
      return throwError(() => new Error('Import files must not exceed 10 MB.'));
    }
    const format = file.name.toLowerCase().endsWith('.csv') ? 'csv' : 'json';
    return from(file.text()).pipe(
      switchMap(data => this.http.post<ImportResult>(`${this.BASE_URL}/${connectionId}/import`, {
        data, format, overwrite: false, database
      })),
      catchError(error => {
        return throwError(
          () => new Error(error?.error?.message || 'Import failed. Ensure file format is correct.')
        );
      })
    );
  }
}
