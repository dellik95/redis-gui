import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialogRef,
  MatDialogTitle,
  MatDialogContent,
  MatDialogActions
} from '@angular/material/dialog';
import { form, FormField, required, validate } from '@angular/forms/signals';
import { UiInputComponent, UiButtonComponent } from '../../ui';

export interface PromptDialogData {
  title?: string;
  message: string;
  placeholder?: string;
  defaultValue?: string;
  confirmText?: string;
  cancelText?: string;
  required?: boolean;
  validator?: (value: string) => string | null;
}

/**
 * Prompt Dialog Component
 * Reusable dialog for text input prompts using Signal Forms
 */
@Component({
  selector: 'app-prompt-dialog',
  standalone: true,
  imports: [
    FormField,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    UiInputComponent,
    UiButtonComponent
  ],
  templateUrl: './prompt-dialog.component.html',
  styleUrl: './prompt-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PromptDialogComponent {
  readonly dialogRef = inject(MatDialogRef<PromptDialogComponent, string | null>);
  readonly data = inject<PromptDialogData>(MAT_DIALOG_DATA);

  readonly title = signal(this.data.title || 'Input');
  readonly message = signal(this.data.message);
  readonly placeholder = signal(this.data.placeholder || '');
  readonly confirmText = signal(this.data.confirmText || 'OK');
  readonly cancelText = signal(this.data.cancelText || 'Cancel');

  readonly promptModel = signal({ value: this.data.defaultValue || '' });

  readonly promptForm = form(this.promptModel, s => {
    if (this.data.required) {
      required(s.value, { message: 'This field is required' });
    }
    if (this.data.validator) {
      validate(s.value, ctx => {
        const err = this.data.validator!(ctx.value() ?? '');
        return err ? { kind: 'custom', message: err } : null;
      });
    }
  });

  readonly canConfirm = computed(() => {
    return this.promptForm().valid();
  });

  onConfirm(event?: Event): void {
    if (event) event.preventDefault();
    if (this.canConfirm()) {
      this.dialogRef.close((this.promptModel().value ?? '').trim());
    }
  }

  onCancel(): void {
    this.dialogRef.close(null);
  }
}
