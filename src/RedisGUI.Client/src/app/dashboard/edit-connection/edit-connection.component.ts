import { ChangeDetectionStrategy, Component, computed, inject, linkedSignal, model, signal } from '@angular/core';
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
import { RedisConnectionsService } from '../../../shared/services/redis-connections.service';
import { CreateConnectionRequest } from '../../../shared/types/connection-request.types';

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
    FormsModule,
    CommonModule],
  templateUrl: './edit-connection.component.html',
  styleUrl: './edit-connection.component.scss'
})
export class EditConnectionComponent {
  readonly data = inject(MAT_DIALOG_DATA);
  readonly connectionService = inject(RedisConnectionsService);
  readonly dialogRef = inject(MatDialogRef<EditConnectionComponent>);

  readonly allDatabasesSelected = model(true);
  readonly hidePasswordMode = signal(true);

  private fb = inject(FormBuilder);

  readonly form: FormGroup = this.fb.group({
    name: [null, [Validators.required], []],
    host: [null, [Validators.required], []],
    port: [6379, [], []],
    database: [0, [], []],
    userName: [null, [], []],
    password: [null, [], []],
  });

  togglePasswordMode(event: MouseEvent) {
    this.hidePasswordMode.set(!this.hidePasswordMode());
    event.stopPropagation();
  }

  checkConnections() {
    var value = this.form.value;

    this.connectionService.checkConnection({
      ...value
    })
      .subscribe({
        next: () => {
          // TODO: Add sucessful notification
         },
        error: () => { }
      });
  }


  saveConnection() {
    if (this.form.valid) {

      let formValue = this.form.value;
      let connection: CreateConnectionRequest = {
        name: formValue.name,
        host: formValue.host,
        port: formValue.port,
        database: 0
      }

      this.connectionService.saveConnection(connection)
        .subscribe({
          next: () => {
            this.dialogRef.close();
          },
          error: () => { }
        });
    }
  }
}
