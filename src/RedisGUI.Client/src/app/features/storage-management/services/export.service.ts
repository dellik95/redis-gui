import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Observable, from, throwError } from 'rxjs';
import { catchError, map, switchMap } from 'rxjs/operators';

async function getExportError(error: unknown): Promise<string> {
  if (error instanceof HttpErrorResponse && error.error instanceof Blob) {
    try {
      const problem = JSON.parse(await error.error.text()) as { detail?: string; message?: string };
      return problem.detail || problem.message || 'Export failed. Please try again.';
    } catch {
      return 'Export failed. Please try again.';
    }
  }
  return 'Export failed. Please try again.';
}

/**
 * Export Service
 * Handles Redis data export operations
 *
 * Provides a clean interface for exporting Redis data to files (JSON/CSV)
 *
 * @example
 * ```typescript
 * private exportService = inject(ExportService);
 *
 * // Export data
 * this.exportService.downloadExport('conn-123', 'json').subscribe({
 *   next: () => console.log('Export successful'),
 *   error: (err) => console.error('Export failed:', err)
 * });
 * ```
 */
@Injectable({
  providedIn: 'root'
})
export class ExportService {
  // 1. Injected dependencies
  private readonly http = inject(HttpClient);

  // 2. Constants
  private readonly BASE_URL = '/api/v1/storage';

  /**
   * Export Redis data to file
   * @param connectionId - The connection ID
   * @param format - Export format: 'json' or 'csv' (default: 'json')
   * @returns Observable with blob data
   */
  exportData(connectionId: string, format: 'json' | 'csv' = 'json'): Observable<Blob> {
    const params = new HttpParams().set('format', format);

    return this.http
      .get(`${this.BASE_URL}/${connectionId}/export`, {
        params,
        responseType: 'blob'
      })
      .pipe(
        catchError(error => from(getExportError(error)).pipe(switchMap(message => throwError(() => new Error(message)))))
      );
  }

  /**
   * Download exported data as file
   * Automatically triggers browser download with proper filename
   *
   * @param connectionId - The connection ID
   * @param format - Export format: 'json' or 'csv' (default: 'json')
   * @returns Observable that completes when download is initiated
   */
  downloadExport(connectionId: string, format: 'json' | 'csv' = 'json', pattern = '*', database = 0): Observable<void> {
    return this.http
      .get(`${this.BASE_URL}/${connectionId}/export`, {
        params: new HttpParams().set('format', format).set('pattern', pattern).set('database', database),
        responseType: 'blob',
        observe: 'response'
      })
      .pipe(
        map(response => {
          const contentDisposition = response.headers.get('Content-Disposition');
          const fileNameMatch = contentDisposition?.match(/filename=(.+)/);
          const fileName = fileNameMatch
            ? fileNameMatch[1]
            : `redis-export-${Date.now()}.${format}`;

          const blob = new Blob([response.body!], {
            type: response.headers.get('Content-Type') || 'application/octet-stream'
          });

          const link = document.createElement('a');
          link.href = window.URL.createObjectURL(blob);
          if (fileName) {
            link.download = fileName;
          }
          document.body.appendChild(link);
          link.click();
          link.remove();
          window.URL.revokeObjectURL(link.href);
        }),
        catchError(error => from(getExportError(error)).pipe(switchMap(message => throwError(() => new Error(message)))))
      );
  }
}
