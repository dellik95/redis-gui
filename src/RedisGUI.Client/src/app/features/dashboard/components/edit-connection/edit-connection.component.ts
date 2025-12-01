import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, model, OnInit, signal } from '@angular/core';
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
  FormBuilder,
  FormGroup,
  FormsModule,
  ReactiveFormsModule,
  Validators
} from "@angular/forms"
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CommonModule } from '@angular/common';
import { StorageConnectionsService } from '../../../../services/api/storage-connections.service';
import { CreateConnectionRequest, UpdateConnectionRequest } from '../../../../models/connection-request.types';
import { ConnectionDialogData } from '../../models/connection-dialog-data.type';
import { Connection as ConnectionType } from '../../../../models/connection.type';
import { ConnectionDialogMode } from '../../models/connection-dialog-mode.enum';
import { ErrorHandlingService } from '../../../../shared/services/error-handling.service';

const defaultFormValue = {
  name: null,
  host: null,
  username: null,
  password: null,
  database: null,
  port: 6379
}


@Component({
  selector: 'app-edit-connection',
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
    FormsModule,
    CommonModule],
  templateUrl: './edit-connection.component.html',
  styleUrl: './edit-connection.component.scss'
})
export class EditConnectionComponent implements OnInit {

  readonly data = inject<ConnectionDialogData>(MAT_DIALOG_DATA);
  readonly connectionService = inject(StorageConnectionsService);
  readonly dialogRef = inject(MatDialogRef<EditConnectionComponent>);
  readonly errorHandler = inject(ErrorHandlingService);

  readonly allDatabasesSelected = model(true);
  readonly hidePasswordMode = signal(true);
  readonly isEditMode = computed(() => this.data.mode === ConnectionDialogMode.EditMode);

  private fb = inject(FormBuilder);

  form!: FormGroup;

  togglePasswordMode(event: MouseEvent) {
    this.hidePasswordMode.set(!this.hidePasswordMode());
    event.stopPropagation();
  }


  ngOnInit(): void {

    let connection = this.data.connection;
    let port = this.getPort(connection);
    let host = this.getHost(connection);

    this.form = this.fb.group({
      name: [connection?.name, [Validators.required], []],
      host: [host, [Validators.required], []],
      port: [port, [], []],
      database: [connection?.database, [], []],
      userName: [connection?.username, [], []],
      password: [connection?.password, [], []],
    });
  }


  getHost(connection: ConnectionType): string | null {

    if (connection == null || connection == undefined) {
      return null;
    }

    const url = new URL(connection.host);
    return url.hostname;
  }

  getPort(connection: ConnectionType): number {
    if (connection == null || connection == undefined) {
      return 6379;
    }

    const url = new URL(connection.host);
    return parseInt(url.port);
  }

  checkConnections() {
    var value = this.form?.value;

    this.connectionService.checkConnection({
      ...value
    })
      .subscribe({
        next: () => {
          this.errorHandler.showSuccess('Connection test successful!');
        },
        error: () => {
          // Error is handled by interceptor
        }
      });
  }


  saveConnection() {
    if (this.form?.valid) {

      let formValue = this.form.value;

      if (this.isEditMode() && this.data.connection?.id) {
        // Update existing connection
        let updateRequest: UpdateConnectionRequest = {
          name: formValue.name,
          host: formValue.host,
          port: formValue.port,
          database: formValue.database || 0,
          username: formValue.userName,
          password: formValue.password
        };

        this.connectionService.updateConnection(this.data.connection.id, updateRequest)
          .subscribe({
            next: () => {
              this.connectionService.clearCache();
              this.errorHandler.showSuccess('Connection updated successfully!');
              this.dialogRef.close(true);
            },
            error: () => {
              // Error is handled by interceptor
            }
          });
      } else {
        // Create new connection
        let createRequest: CreateConnectionRequest = {
          name: formValue.name,
          host: formValue.host,
          port: formValue.port,
          database: formValue.database || 0,
          username: formValue.userName,
          password: formValue.password
        };

        this.connectionService.saveConnection(createRequest)
          .subscribe({
            next: () => {
              this.connectionService.clearCache();
              this.errorHandler.showSuccess('Connection created successfully!');
              this.dialogRef.close(true);
            },
            error: () => {
              // Error is handled by interceptor
            }
          });
      }
    }
  }
}
