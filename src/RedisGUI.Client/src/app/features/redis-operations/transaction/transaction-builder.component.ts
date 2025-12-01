import { Component, inject, signal, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RedisOperationsService } from '../../../services/api/redis-operations.service';
import { ExecuteTransactionRequest, RedisOperation } from '../../../models/redis-advanced.types';

interface OperationDraft {
  command: string; // SET | GET | DEL | INCR | DECR
  key: string;
  value?: string;
}

@Component({
  selector: 'app-transaction-builder',
  standalone: true,
  templateUrl: './transaction-builder.component.html',
  styleUrls: ['./transaction-builder.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule
  ]
})
export class TransactionBuilderComponent {
  private redisOperationsService = inject(RedisOperationsService);
  private snackBar = inject(MatSnackBar);

  connectionId = input.required<string>();

  operations = signal<OperationDraft[]>([
    { command: 'SET', key: '', value: '' }
  ]);

  executing = signal<boolean>(false);
  result = signal<{ success: boolean; results: string[]; operationsExecuted: number } | null>(null);

      updateCommand(operation: OperationDraft, command: string): void {
        this.operations.update(list => {
          const idx = list.findIndex(op => op === operation);
          if (idx === -1) return list;
          const copy = [...list];
          copy[idx] = { ...copy[idx], command };
          return copy;
        });
      }

      updateKey(operation: OperationDraft, key: string): void {
        this.operations.update(list => {
          const idx = list.findIndex(op => op === operation);
          if (idx === -1) return list;
          const copy = [...list];
          copy[idx] = { ...copy[idx], key };
          return copy;
        });
      }

      updateValue(operation: OperationDraft, value: string): void {
        this.operations.update(list => {
          const idx = list.findIndex(op => op === operation);
          if (idx === -1) return list;
          const copy = [...list];
          copy[idx] = { ...copy[idx], value };
          return copy;
        });
      }


    addOperation(afterOperation?: OperationDraft): void {
      const newOp: OperationDraft = { command: 'GET', key: '' };
      this.operations.update(list => {
        if (!afterOperation) {
          return [...list, newOp];
        }
        const idx = list.findIndex(op => op === afterOperation);
        if (idx === -1) return [...list, newOp];
        const copy = [...list];
        copy.splice(idx + 1, 0, newOp);
        return copy;
      });
    }

    removeOperation(operation: OperationDraft): void {
      this.operations.update(list => list.filter(op => op !== operation));
    }

    moveUp(operation: OperationDraft): void {
      this.operations.update(list => {
        const idx = list.findIndex(op => op === operation);
        if (idx <= 0) return list;
        const copy = [...list];
        const temp = copy[idx - 1];
        copy[idx - 1] = copy[idx];
        copy[idx] = temp;
        return copy;
      });
    }

    moveDown(operation: OperationDraft): void {
      this.operations.update(list => {
        const idx = list.findIndex(op => op === operation);
        if (idx == null || idx >= list.length - 1) return list;
        const copy = [...list];
        const temp = copy[idx + 1];
        copy[idx + 1] = copy[idx];
        copy[idx] = temp;
        return copy;
      });
    }

  clear(): void {
    this.operations.set([{ command: 'SET', key: '', value: '' }]);
    this.result.set(null);
  }

  isValueRequired(op: OperationDraft): boolean {
    return op.command === 'SET';
  }

  validate(): string[] {
    const errors: string[] = [];
    const ops = this.operations();
    if (ops.length === 0) errors.push('At least one operation required');

    ops.forEach((op, i) => {
      if (!op.key.trim()) errors.push(`Row ${i + 1}: key required`);
      if (this.isValueRequired(op) && !op.value?.trim()) errors.push(`Row ${i + 1}: value required for SET`);
      if (!['SET', 'GET', 'DEL', 'INCR', 'DECR'].includes(op.command)) errors.push(`Row ${i + 1}: invalid command '${op.command}'`);
    });

    if (ops.length > 100) errors.push('Max 100 operations per transaction');
    return errors;
  }

  execute(): void {
    const errors = this.validate();
    if (errors.length) {
      this.snackBar.open(errors[0], 'Close', { duration: 4000 });
      return;
    }
    this.executing.set(true);
    this.result.set(null);

    const request: ExecuteTransactionRequest = {
      operations: this.operations().map<RedisOperation>(o => ({
        command: o.command,
        key: o.key,
        value: o.value?.trim() || undefined
      }))
    };

    this.redisOperationsService.executeTransaction(this.connectionId(), request).subscribe({
      next: resp => {
        this.result.set(resp);
        this.snackBar.open(`Executed ${resp.operationsExecuted} operations`, 'Close', { duration: 4000 });
      },
      error: err => {
        this.snackBar.open(`Transaction failed: ${err.message}`, 'Close', { duration: 5000 });
      },
      complete: () => this.executing.set(false)
    });
  }
}
