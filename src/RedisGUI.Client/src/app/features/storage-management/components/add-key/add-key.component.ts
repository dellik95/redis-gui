import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { RedisDataType } from '../../../../models/redis-keys.types';
import { StorageValueEditorComponent, ValueEditorInput, ValueEditorOutput } from '../storage-value-editor/storage-value-editor.component';

/**
 * Dialog data for Add Key component
 */
export interface AddKeyDialogData {
  /** Redis connection ID */
  connectionId: string;
}

/**
 * Add Key Component
 * Modal dialog for creating new Redis keys
 *
 * Features:
 * - Key name input
 * - Data type selection
 * - Value editor for all Redis types
 * - TTL configuration
 *
 * @example
 * ```typescript
 * this.dialog.open(AddKeyComponent, {
 *   data: { connectionId: 'conn-123' }
 * });
 * ```
 */
@Component({
  selector: 'app-add-key',
  standalone: true,
  imports: [StorageValueEditorComponent],
  templateUrl: './add-key.component.html',
  styleUrl: './add-key.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddKeyComponent {
  // 1. Injected dependencies
  private readonly dialogRef = inject<MatDialogRef<AddKeyComponent, ValueEditorOutput | undefined>>(MatDialogRef);
  private readonly dialogData = inject<AddKeyDialogData>(MAT_DIALOG_DATA);

  // 2. Configuration
  /** Redis connection ID from dialog data */
  readonly connectionId = this.dialogData.connectionId;

  /** Initial editor data for new key */
  readonly editorData: ValueEditorInput = {
    key: '',
    type: RedisDataType.String,
    value: { value: '' },
    isNew: true
  };

  // 3. Public methods
  /**
   * Handles save action from value editor
   * Closes dialog with the new key data
   *
   * @param data - Editor output with new key data
   */
  onSave(data: ValueEditorOutput): void {
    this.dialogRef.close(data);
  }

  /**
   * Handles cancel action
   * Closes dialog without saving
   */
  onCancel(): void {
    this.dialogRef.close();
  }
}

