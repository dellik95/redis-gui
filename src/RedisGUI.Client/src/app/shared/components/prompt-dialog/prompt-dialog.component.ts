import { Component, inject, signal, computed, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

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
 * Reusable dialog for text input prompts
 * 
 * @example
 * ```typescript
 * this.dialog.open(PromptDialogComponent, {
 *   data: {
 *     title: 'Edit Parameter',
 *     message: 'Enter new value:',
 *     placeholder: 'Value',
 *     defaultValue: 'current value',
 *     required: true
 *   }
 * }).afterClosed().subscribe(result => {
 *   if (result) {
 *     // Use result
 *   }
 * });
 * ```
 */
@Component({
	selector: 'app-prompt-dialog',
	standalone: true,
	imports: [
		ReactiveFormsModule,
		MatButtonModule,
		MatFormFieldModule,
		MatInputModule,
		MatDialogTitle,
		MatDialogContent,
		MatDialogActions
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

	readonly inputCtrl = new FormControl(this.data.defaultValue || '', {
		nonNullable: true,
		validators: this.data.required ? [Validators.required] : []
	});

	readonly canConfirm = computed(() => {
		if (this.data.required && !this.inputCtrl.value.trim()) return false;
		if (this.data.validator) {
			return this.data.validator(this.inputCtrl.value) === null;
		}
		return true;
	});

	readonly errorMessage = computed(() => {
		if (this.inputCtrl.hasError('required')) return 'This field is required';
		if (this.data.validator) {
			return this.data.validator(this.inputCtrl.value) || null;
		}
		return null;
	});

	onConfirm(): void {
		if (this.canConfirm()) {
			this.dialogRef.close(this.inputCtrl.value.trim());
		}
	}

	onCancel(): void {
		this.dialogRef.close(null);
	}
}

