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
    const config: MatSnackBarConfig = {
      ...this.defaultConfig,
      panelClass: ['error-snackbar']
    };
    if (duration !== undefined) {
      config.duration = duration;
    }
    this.snackBar.open(message, 'Close', config);
  }

  /**
   * Shows a success message to the user
   */
  showSuccess(message: string, duration?: number): void {
    const config: MatSnackBarConfig = {
      ...this.defaultConfig,
      panelClass: ['success-snackbar']
    };
    if (duration !== undefined) {
      config.duration = duration;
    }
    this.snackBar.open(message, 'Close', config);
  }

  /**
   * Shows a warning message to the user
   */
  showWarning(message: string, duration?: number): void {
    const config: MatSnackBarConfig = {
      ...this.defaultConfig,
      panelClass: ['warning-snackbar']
    };
    if (duration !== undefined) {
      config.duration = duration;
    }
    this.snackBar.open(message, 'Close', config);
  }

  /**
   * Shows an info message to the user
   */
  showInfo(message: string, duration?: number): void {
    const config: MatSnackBarConfig = {
      ...this.defaultConfig,
      panelClass: ['info-snackbar']
    };
    if (duration !== undefined) {
      config.duration = duration;
    }
    this.snackBar.open(message, 'Close', config);
  }
}
