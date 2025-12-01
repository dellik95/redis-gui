import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

/**
 * Service for handling and displaying errors to users
 */
@Injectable({
  providedIn: 'root'
})
export class ErrorHandlingService {
  private snackBar = inject(MatSnackBar);

  private readonly defaultConfig: MatSnackBarConfig = {
    duration: 5000,
    horizontalPosition: 'end',
    verticalPosition: 'top'
  };

  /**
   * Shows an error message to the user
   */
  showError(message: string, duration?: number): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      duration: duration || this.defaultConfig.duration,
      panelClass: ['error-snackbar']
    });
  }

  /**
   * Shows a success message to the user
   */
  showSuccess(message: string, duration?: number): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      duration: duration || this.defaultConfig.duration,
      panelClass: ['success-snackbar']
    });
  }

  /**
   * Shows a warning message to the user
   */
  showWarning(message: string, duration?: number): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      duration: duration || this.defaultConfig.duration,
      panelClass: ['warning-snackbar']
    });
  }

  /**
   * Shows an info message to the user
   */
  showInfo(message: string, duration?: number): void {
    this.snackBar.open(message, 'Close', {
      ...this.defaultConfig,
      duration: duration || this.defaultConfig.duration,
      panelClass: ['info-snackbar']
    });
  }
}
