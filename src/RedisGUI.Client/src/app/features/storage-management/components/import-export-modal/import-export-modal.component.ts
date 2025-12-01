import { Component, inject, signal, input, effect, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';
import { RedisOperationsService } from '../../../../services/api/redis-operations.service';
import { MatOptionModule } from '@angular/material/core';

@Component({
  selector: 'app-import-export-modal',
  standalone: true,
  templateUrl: './import-export-modal.component.html',
  styleUrls: ['./import-export-modal.component.scss'],
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatFormFieldModule,
    MatInputModule,
    MatCheckboxModule,
    MatProgressBarModule,
    MatTooltipModule,
    FormsModule,
	MatOptionModule
  ]
})

export class ImportExportModalComponent {
  @Output() completed = new EventEmitter<any>();
    // Handler for overwrite checkbox
    updateOverwrite(value: boolean): void {
      this.overwrite.set(value);
    }

    // Handler for file format select
    updateFileFormat(value: 'json' | 'csv'): void {
      this.fileFormat.set(value);
    }
  connectionId = input.required<string>();
  mode = input<string>('import'); // 'import' or 'export'
  pattern = input<string>('*');

  fileName = signal<string>('');
  fileContent = signal<string>('');
  fileFormat = signal<'json' | 'csv'>('json');
  overwrite = signal<boolean>(false);
  previewRows = signal<any[]>([]);
  previewError = signal<string | null>(null);
  importProgress = signal<number>(0);
  importResult = signal<any | null>(null);
  loading = signal<boolean>(false);

  dialogRef = inject(MatDialogRef<ImportExportModalComponent>);
  api = inject(RedisOperationsService);


  onFileDrop(event: DragEvent): void {
    event.preventDefault();
    const file = event.dataTransfer?.files?.[0];
    if (file) this.readFile(file);
  }
  onFileInput(event: any): void {
    const file = event.target.files?.[0];
    if (file) this.readFile(file);
  }
  readFile(file: File): void {
    this.fileName.set(file.name);
    this.fileFormat.set(file.name.endsWith('.csv') ? 'csv' : 'json');
    const reader = new FileReader();
    reader.onload = (e: any) => {
      this.fileContent.set(e.target.result);
      this.previewFile();
    };
    reader.readAsText(file);
  }
  previewFile(): void {
    this.previewError.set(null);
    try {
      if (this.fileFormat() === 'json') {
        const parsed = JSON.parse(this.fileContent());
        this.previewRows.set(Array.isArray(parsed) ? parsed.slice(0, 10) : [parsed]);
      } else {
        // Simple CSV preview: split lines
        const lines = this.fileContent().split(/\r?\n/).filter(l => l.trim());
        this.previewRows.set(lines.slice(0, 10));
      }
    } catch (err: any) {
      this.previewError.set('Failed to parse file: ' + err.message);
      this.previewRows.set([]);
    }
  }
  importData(): void {
    this.loading.set(true);
    this.importProgress.set(0);
    this.api.importData(this.connectionId(), {
      data: this.fileContent(),
      format: this.fileFormat(),
      overwrite: this.overwrite()
    }).subscribe({
      next: result => {
        this.importResult.set(result);
        this.completed.emit(result);
      },
      error: err => {
        this.importResult.set({ error: err.message });
      },
      complete: () => this.loading.set(false)
    });
  }
  close(): void { this.dialogRef.close(); }
}
