import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';

/** Downloads a completed, authorized background export. */
@Injectable({ providedIn: 'root' })
export class ExportService {
  private readonly http = inject(HttpClient);

  downloadExport(jobId: string, format: 'json' | 'csv'): Observable<void> {
    return this.http.get(`/api/v1/jobs/${jobId}/result`, { responseType: 'blob', observe: 'response' }).pipe(
      map(response => {
        const contentDisposition = response.headers.get('Content-Disposition');
        const match = contentDisposition?.match(/filename="?([^";]+)"?/i);
        const url = URL.createObjectURL(response.body!);
        const link = document.createElement('a');
        link.href = url;
        link.download = match?.[1] || `redis-export-${Date.now()}.${format}`;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
      })
    );
  }
}
