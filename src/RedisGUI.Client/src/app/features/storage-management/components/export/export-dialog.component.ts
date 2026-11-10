import { Component, inject, signal, DestroyRef, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { form, FormField, submit } from '@angular/forms/signals';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs/operators';
import { ExportService } from '../../services/export.service';
import { UiRadioGroupComponent, UiRadioOption, UiButtonComponent } from '../../../../shared/ui';

/**
 * Export Dialog Component
 * Dialog for exporting Redis data using Signal Forms and UiRadioGroup
 */
@Component({
  selector: 'app-export-dialog',
  standalone: true,
  imports: [
    FormField,
    MatProgressSpinnerModule,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    UiRadioGroupComponent,
    UiButtonComponent
  ],
  templateUrl: './export-dialog.component.html',
  styleUrl: './export-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExportDialogComponent {
  private readonly exportService = inject(ExportService);
  private readonly destroyRef = inject(DestroyRef);
  readonly dialogRef = inject(MatDialogRef<ExportDialogComponent>);
  private readonly data = inject<{ connectionId: string }>(MAT_DIALOG_DATA);

  /** Signal form model */
  readonly exportModel = signal({ format: 'json' as 'json' | 'csv' });
  readonly exportForm = form(this.exportModel);

  readonly formatOptions: readonly UiRadioOption<'json' | 'csv'>[] = [
    {
      label: 'JSON format',
      description: 'Full data structure with keys, data types, and TTL metadata',
      value: 'json'
    },
    {
      label: 'CSV format',
      description: 'Tabular representation of keys, types, and serialized values',
      value: 'csv'
    }
  ];

  readonly isLoading = signal<boolean>(false);
  readonly error = signal<string | null>(null);

  onExport(): void {
    this.error.set(null);
    this.isLoading.set(true);

    submit(this.exportForm, async () => {
      const format = this.exportModel().format;
      
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
    });
  }
}
