import { ChangeDetectionStrategy, Component, computed, inject, model, signal, DestroyRef } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import {
  MAT_DIALOG_DATA,
  MatDialogTitle,
  MatDialogContent,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import {
  FormControl,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators
} from "@angular/forms"
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateConnectionRequest, UpdateConnectionRequest, CheckConnectionRequest } from '../../../../models/connection-request.types';
import { ConnectionDialogData } from '../../models/connection-dialog-data.type';
import { Connection as ConnectionType } from '../../../../models/connection.type';
import { ConnectionDialogMode } from '../../models/connection-dialog-mode.enum';
import { ErrorHandlingService } from '../../../../shared/services/error-handling.service';
import { ConnectionsStore } from '../../../../core/stores/connections.store';

/**
 * Edit Connection Component
 * Dialog component for creating or editing Redis connections
 * 
 * Features:
 * - Create new connections
 * - Edit existing connections
 * - Connection testing
 * - Form validation
 * - Password visibility toggle
 * - Database selection
 * - Modern reactive forms without FormBuilder
 * 
 * @example
 * ```typescript
 * // Open dialog
 * this.dialog.open(EditConnectionComponent, {
 *   data: { mode: ConnectionDialogMode.CreateMode, connection: null }
 * });
 * ```
 */
@Component({
  selector: 'app-edit-connection',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatDialogTitle,
    MatDialogContent,
    MatExpansionModule,
    MatDividerModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatProgressSpinnerModule,
    FormsModule
  ],
  templateUrl: './edit-connection.component.html',
  styleUrl: './edit-connection.component.scss'
})
export class EditConnectionComponent {
  // 1. Injected dependencies
  private readonly data = inject<ConnectionDialogData>(MAT_DIALOG_DATA);
  private readonly connectionsStore = inject(ConnectionsStore);
  private readonly dialogRef = inject(MatDialogRef<EditConnectionComponent>);
  private readonly errorHandler = inject(ErrorHandlingService);
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);

  // 2. State signals
  /** Whether all databases are selected */
  readonly allDatabasesSelected = model(true);
  
  /** Password visibility toggle state */
  readonly hidePasswordMode = signal(true);
  
  /** Whether a connection test is in progress */
  readonly isTesting = signal(false);

  // 3. Computed signals
  /** Whether component is in edit mode (vs create mode) */
  readonly isEditMode = computed(() => this.data.mode === ConnectionDialogMode.EditMode);

  /** Dialog title based on mode */
  readonly dialogTitle = computed(() => 
    this.isEditMode() ? 'Edit Connection' : 'New Connection'
  );

  // 4. Form definition (without FormBuilder)
  /** Connection form with validation */
  readonly form = this.createForm();

  // 5. Computed signals for form state
  /** Whether the form can be saved */
  readonly canSave = computed(() => 
    this.form.valid && this.form.dirty
  );

  /** Whether connection can be tested */
  readonly canTest = computed(() => 
    this.form.valid && !this.isTesting()
  );

  /** Name field error message */
  readonly nameError = computed(() => {
    const control = this.form.get('name');
    if (!control || !control.touched) return null;
    if (control.hasError('required')) return 'Connection name is required';
    return null;
  });

  /** Host field error message */
  readonly hostError = computed(() => {
    const control = this.form.get('host');
    if (!control || !control.touched) return null;
    if (control.hasError('required')) return 'Host is required';
    return null;
  });

  /**
   * Create and initialize form
   */
  private createForm(): FormGroup {
    const connection = this.data.connection ?? null;
    const port = this.getPort(connection);
    const host = this.getHost(connection);

    return new FormGroup({
      name: new FormControl(connection?.name ?? '', {
        nonNullable: true,
        validators: [Validators.required]
      }),
      host: new FormControl(host ?? '', {
        nonNullable: true,
        validators: [Validators.required]
      }),
      port: new FormControl(port, {
        nonNullable: true
      }),
      database: new FormControl(connection?.database ?? 0, {
        nonNullable: true
      }),
      userName: new FormControl(connection?.username ?? '', {
        nonNullable: true
      }),
      password: new FormControl(connection?.password ?? '', {
        nonNullable: true
      })
    });
  }

  /**
   * Toggle password visibility
   */
  togglePasswordMode(event: MouseEvent): void {
    this.hidePasswordMode.set(!this.hidePasswordMode());
    event.stopPropagation();
  }

  /**
   * Extract hostname from connection URL
   */
  private getHost(connection: ConnectionType | null | undefined): string | null {
    if (!connection) {
      return null;
    }

    try {
      const url = new URL(connection.host);
      return url.hostname;
    } catch {
      return connection.host;
    }
  }

  /**
   * Extract port from connection URL, defaulting to 6379
   */
  private getPort(connection: ConnectionType | null | undefined): number {
    if (!connection) {
      return 6379;
    }

    try {
      const url = new URL(connection.host);
      const port = parseInt(url.port, 10);
      return isNaN(port) ? 6379 : port;
    } catch {
      return 6379;
    }
  }

  /**
   * Test the connection with current form values
   */
  checkConnections(): void {
    if (!this.canTest()) return;

    this.isTesting.set(true);

    const checkRequest: CheckConnectionRequest = {
      host: this.form.get('host')?.value ?? '',
      port: this.form.get('port')?.value ?? 6379,
      database: this.form.get('database')?.value ?? 0,
      username: this.form.get('userName')?.value || undefined,
      password: this.form.get('password')?.value || undefined
    };

    this.http.post<{ success: boolean }>('/api/v1/connections/check', checkRequest)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.isTesting.set(false);
          this.errorHandler.showSuccess('Connection test successful!');
        },
        error: () => {
          this.isTesting.set(false);
          // Error is handled by interceptor
        }
      });
  }

  /**
   * Save or update the connection
   */
  saveConnection(): void {
    if (!this.canSave()) return;

    if (this.isEditMode() && this.data.connection?.id) {
      // Update existing connection
      const updateRequest: UpdateConnectionRequest = {
        id: this.data.connection.id,
        name: this.form.get('name')?.value ?? '',
        host: this.form.get('host')?.value ?? '',
        port: this.form.get('port')?.value ?? 6379,
        database: this.form.get('database')?.value ?? 0,
        username: this.form.get('userName')?.value || undefined,
        password: this.form.get('password')?.value || undefined
      };

      this.connectionsStore.updateConnection(updateRequest);
      this.errorHandler.showSuccess('Connection updated successfully!');
      this.dialogRef.close(true);
    } else {
      // Create new connection
      const createRequest: CreateConnectionRequest = {
        name: this.form.get('name')?.value ?? '',
        host: this.form.get('host')?.value ?? '',
        port: this.form.get('port')?.value ?? 6379,
        database: this.form.get('database')?.value ?? 0,
        username: this.form.get('userName')?.value || undefined,
        password: this.form.get('password')?.value || undefined
      };

      this.connectionsStore.createConnection(createRequest);
      this.errorHandler.showSuccess('Connection created successfully!');
      this.dialogRef.close(true);
    }
  }
}
