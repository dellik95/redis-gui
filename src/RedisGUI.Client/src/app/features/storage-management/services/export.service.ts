import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

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

		return this.http.get(`${this.BASE_URL}/${connectionId}/export`, {
			params,
			responseType: 'blob'
		}).pipe(
			catchError((error) => {
				return throwError(() =>
					new Error(error?.error?.message || 'Export failed. Please try again.')
				);
			})
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
	downloadExport(connectionId: string, format: 'json' | 'csv' = 'json'): Observable<void> {
		return this.http.get(`${this.BASE_URL}/${connectionId}/export`, {
			params: new HttpParams().set('format', format),
			responseType: 'blob',
			observe: 'response'
		}).pipe(
			map((response) => {
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
			catchError((error) => {
				return throwError(() =>
					new Error(error?.error?.message || 'Export failed. Please try again.')
				);
			})
		);
	}
}

