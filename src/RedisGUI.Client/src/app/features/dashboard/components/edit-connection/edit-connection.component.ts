import { ChangeDetectionStrategy, Component, computed, inject, model, signal, DestroyRef } from '@angular/core';
import {
  MAT_DIALOG_DATA,
  MatDialogTitle,
  MatDialogContent,
  MatDialogActions,
  MatDialogRef,
} from '@angular/material/dialog';
import { MatDividerModule } from '@angular/material/divider';
import { MatExpansionModule } from '@angular/material/expansion';
import { form, FormField, required, min, max, submit } from '@angular/forms/signals';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { HttpClient } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CreateConnectionRequest, UpdateConnectionRequest, CheckConnectionRequest } from '../../../../models/connection-request.types';
import { ConnectionDialogData } from '../../models/connection-dialog-data.type';
import { Connection as ConnectionType } from '../../../../models/connection.type';
import { ConnectionDialogMode } from '../../models/connection-dialog-mode.enum';
import { ErrorHandlingService } from '../../../../shared/services/error-handling.service';
import { ConnectionsStore } from '../../../../core/stores/connections.store';
import { UiInputComponent, UiCheckboxComponent } from '../../../../shared/ui';

/**
 * Edit Connection Component
 * Dialog component for creating or editing Redis connections using Signal Forms
 */
@Component({
  selector: 'app-edit-connection',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormField,
    MatDialogTitle,
    MatDialogContent,
    MatDialogActions,
    MatExpansionModule,
    MatDividerModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    UiInputComponent,
    UiCheckboxComponent
  ],
  templateUrl: './edit-connection.component.html',
  styleUrl: './edit-connection.component.scss'
})
export class EditConnectionComponent {
  private readonly data = inject<ConnectionDialogData>(MAT_DIALOG_DATA);
  private readonly connectionsStore = inject(ConnectionsStore);
  private readonly dialogRef = inject(MatDialogRef<EditConnectionComponent>);
  private readonly errorHandler = inject(ErrorHandlingService);
  private readonly http = inject(HttpClient);
  private readonly destroyRef = inject(DestroyRef);

  /** Whether all databases are selected */
  readonly allDatabasesSelected = model(true);
  
  /** Password visibility toggle state */
  readonly hidePasswordMode = signal(true);
  
  /** Whether a connection test is in progress */
  readonly isTesting = signal(false);

  /** Whether component is in edit mode (vs create mode) */
  readonly isEditMode = computed(() => this.data.mode === ConnectionDialogMode.EditMode);

  /** Dialog title based on mode */
  readonly dialogTitle = computed(() => 
    this.isEditMode() ? 'Edit Connection' : 'New Redis Connection'
  );

  /** Signal form model */
  readonly connectionModel = signal({
    name: this.data.mode === ConnectionDialogMode.EditMode ? (this.data.connection?.name ?? '') : 'Local Redis',
    host: this.data.mode === ConnectionDialogMode.EditMode ? (this.getHost(this.data.connection) ?? '') : 'localhost',
    port: this.data.mode === ConnectionDialogMode.EditMode ? this.getPort(this.data.connection) : 6379,
    database: this.data.connection?.database ?? 0,
    userName: this.data.connection?.username ?? '',
    password: this.data.connection?.password ?? ''
  });

  /** Signal Form with declarative schema rules */
  readonly connectionForm = form(this.connectionModel, (s) => {
    required(s.name, { message: 'Connection name is required' });
    required(s.host, { message: 'Host is required' });
    required(s.port, { message: 'Port is required' });
    min(s.port, 1, { message: 'Port must be between 1 and 65535' });
    max(s.port, 65535, { message: 'Port must be between 1 and 65535' });
  });

  /** Whether the form can be saved - evaluated reactively */
  readonly canSave = computed(() => {
    const m = this.connectionModel();
    return !!m.name.trim() && !!m.host.trim() && m.port >= 1 && m.port <= 65535 && this.connectionForm().valid();
  });

  /** Whether connection can be tested */
  readonly canTest = computed(() => {
    return this.canSave() && !this.isTesting();
  });

  togglePasswordMode(event?: MouseEvent): void {
    this.hidePasswordMode.set(!this.hidePasswordMode());
    event?.stopPropagation();
  }

  cancel(): void {
    this.dialogRef.close(false);
  }

  private getHost(connection: ConnectionType | null | undefined): string | null {
    if (!connection) return null;
    try {
      const url = new URL(connection.host);
      return url.hostname;
    } catch {
      return connection.host;
    }
  }

  private getPort(connection: ConnectionType | null | undefined): number {
    if (!connection) return 6379;
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

    const m = this.connectionModel();
    const checkRequest: CheckConnectionRequest = {
      host: m.host,
      port: m.port,
      database: this.allDatabasesSelected() ? 0 : m.database,
      username: m.userName ? m.userName : null,
      password: m.password ? m.password : null
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
        }
      });
  }

  /**
   * Save or update the connection using Signal Forms submit()
   */
  saveConnection(event?: Event): void {
    if (event) event.preventDefault();
    if (!this.canSave()) return;

    submit(this.connectionForm, async () => {
      const m = this.connectionModel();
      const db = this.allDatabasesSelected() ? 0 : m.database;

      if (this.isEditMode() && this.data.connection?.id) {
        const updateRequest: UpdateConnectionRequest = {
          id: this.data.connection.id,
          name: m.name,
          host: m.host,
          port: m.port,
          database: db,
          username: m.userName ? m.userName : null,
          password: m.password ? m.password : null
        };

        this.connectionsStore.updateConnection(updateRequest);
        this.errorHandler.showSuccess('Connection updated successfully!');
        this.dialogRef.close(true);
      } else {
        const createRequest: CreateConnectionRequest = {
          name: m.name,
          host: m.host,
          port: m.port,
          database: db,
          username: m.userName ? m.userName : null,
          password: m.password ? m.password : null
        };

        this.connectionsStore.createConnection(createRequest);
        this.errorHandler.showSuccess('Connection created successfully!');
        this.dialogRef.close(true);
      }
    });
  }
}
