import { Component, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogTitle, MatDialogContent, MatDialogActions } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';

export interface ConfirmDialogData {
	title?: string;
	message: string;
	confirmText?: string;
	cancelText?: string;
	confirmColor?: 'primary' | 'accent' | 'warn';
}

/**
 * Confirmation Dialog Component
 * Reusable dialog for user confirmations
 * 
 * @example
 * ```typescript
 * this.dialog.open(ConfirmDialogComponent, {
 *   data: {
 *     title: 'Delete Key',
 *     message: 'Are you sure you want to delete this key?',
 *     confirmText: 'Delete',
 *     cancelText: 'Cancel',
 *     confirmColor: 'warn'
 *   }
 * });
 * ```
 */
@Component({
	selector: 'app-confirm-dialog',
	standalone: true,
	imports: [
		MatButtonModule,
		MatDialogTitle,
		MatDialogContent,
		MatDialogActions
	],
	templateUrl: './confirm-dialog.component.html',
	styleUrl: './confirm-dialog.component.scss',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfirmDialogComponent {
	readonly dialogRef = inject(MatDialogRef<ConfirmDialogComponent, boolean>);
	readonly data = inject<ConfirmDialogData>(MAT_DIALOG_DATA);

	readonly title = signal(this.data.title || 'Confirm');
	readonly message = signal(this.data.message);
	readonly confirmText = signal(this.data.confirmText || 'Confirm');
	readonly cancelText = signal(this.data.cancelText || 'Cancel');
	readonly confirmColor = signal(this.data.confirmColor || 'primary');

	onConfirm(): void {
		this.dialogRef.close(true);
	}

	onCancel(): void {
		this.dialogRef.close(false);
	}
}

