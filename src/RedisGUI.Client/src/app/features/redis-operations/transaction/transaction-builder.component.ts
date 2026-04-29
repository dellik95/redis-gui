import { Component, inject, signal, input, computed, ChangeDetectionStrategy, effect, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';

import { RedisOperationsStore } from '../../../core/stores/redis-operations.store';
import { ExecuteTransactionRequest, RedisOperation } from '../../../models/redis-advanced.types';

/**
 * Draft operation for transaction builder
 */
interface OperationDraft {
  /** Redis command: SET, GET, DEL, INCR, DECR */
  command: string;
  /** Redis key */
  key: string;
  /** Value (for SET command) */
  value?: string;
}

/**
 * Transaction Builder Component
 * Builds and executes Redis transactions (MULTI/EXEC)
 *
 * Features:
 * - Add/remove operations
 * - Configure operation parameters
 * - Execute transaction atomically
 * - View execution results
 *
 * @example
 * ```html
 * <app-transaction-builder [connectionId]="connectionId" />
 * ```
 */
@Component({
  selector: 'app-transaction-builder',
  standalone: true,
  templateUrl: './transaction-builder.component.html',
  styleUrl: './transaction-builder.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
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
  // 1. Injected dependencies
  private readonly redisOperationsStore = inject(RedisOperationsStore);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);

  // 2. Inputs
  readonly connectionId = input.required<string>();

  // 3. State signals
  readonly operations = signal<OperationDraft[]>([
    { command: 'SET', key: '', value: '' }
  ]);
  readonly executing = signal<boolean>(false);
  readonly result = signal<{ success: boolean; results: string[]; operationsExecuted: number } | null>(null);
  private readonly executionPending = signal<boolean>(false);

  // 4. Computed signals
  /** Whether the transaction can be executed (all operations have keys and not currently executing) */
  readonly canExecute = computed(() => {
    return this.operations().length > 0 &&
           this.operations().every(op => op.key.trim().length > 0) &&
           !this.executing();
  });

  // 5. Public methods
  /**
   * Updates the command for an operation
   *
   * @param operation - Operation draft to update
   * @param command - New command value
   */
  updateCommand(operation: OperationDraft, command: string): void {
        this.operations.update(list => {
          const idx = list.findIndex(op => op === operation);
          if (idx === -1) return list;
          const copy = [...list];
          const existing = copy[idx]!;
          const updated: OperationDraft = { command, key: existing.key };
          if (existing.value !== undefined) {
            updated.value = existing.value;
          }
          copy[idx] = updated;
          return copy;
        });
      }

  /**
   * Updates the key for an operation
   *
   * @param operation - Operation draft to update
   * @param key - New key value
   */
  updateKey(operation: OperationDraft, key: string): void {
        this.operations.update(list => {
          const idx = list.findIndex(op => op === operation);
          if (idx === -1) return list;
          const copy = [...list];
          const existing = copy[idx]!;
          const updated: OperationDraft = { command: existing.command, key };
          if (existing.value !== undefined) {
            updated.value = existing.value;
          }
          copy[idx] = updated;
          return copy;
        });
      }

  /**
   * Updates the value for an operation
   *
   * @param operation - Operation draft to update
   * @param value - New value
   */
  updateValue(operation: OperationDraft, value: string): void {
        this.operations.update(list => {
          const idx = list.findIndex(op => op === operation);
          if (idx === -1) return list;
          const copy = [...list];
          const existing = copy[idx]!;
          copy[idx] = { command: existing.command, key: existing.key, value };
          return copy;
        });
      }


  /**
   * Adds a new operation to the transaction
   *
   * @param afterOperation - Optional operation to insert after (otherwise appends)
   */
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

  /**
   * Removes an operation from the transaction
   *
   * @param operation - Operation to remove
   */
  removeOperation(operation: OperationDraft): void {
      this.operations.update(list => list.filter(op => op !== operation));
    }

  /**
   * Moves an operation up in the list
   *
   * @param operation - Operation to move up
   */
  moveUp(operation: OperationDraft): void {
      this.operations.update(list => {
        const idx = list.findIndex(op => op === operation);
        if (idx <= 0) return list;
        const copy = [...list];
        const temp = copy[idx - 1]!;
        copy[idx - 1] = copy[idx]!;
        copy[idx] = temp;
        return copy;
      });
    }

  /**
   * Moves an operation down in the list
   *
   * @param operation - Operation to move down
   */
  moveDown(operation: OperationDraft): void {
      this.operations.update(list => {
        const idx = list.findIndex(op => op === operation);
        if (idx === -1 || idx >= list.length - 1) return list;
        const copy = [...list];
        const temp = copy[idx + 1]!;
        copy[idx + 1] = copy[idx]!;
        copy[idx] = temp;
        return copy;
      });
    }

  // 6. Constructor with effects
  constructor() {
    // Watch for transaction execution results
    effect(() => {
      if (!this.executionPending()) return;

      const resp = this.redisOperationsStore.lastTransactionResult();
      if (resp) {
        this.result.set(resp);
        this.snackBar.open(`Executed ${resp.operationsExecuted} operations`, 'Close', { duration: 4000 });
        this.executing.set(false);
        this.executionPending.set(false);
      }

      const err = this.redisOperationsStore.error();
      if (err) {
        this.snackBar.open(`Transaction failed: ${err}`, 'Close', { duration: 5000 });
        this.executing.set(false);
        this.executionPending.set(false);
      }
    }, { allowSignalWrites: true });
  }

  /**
   * Clears all operations and resets to default state
   */
  clear(): void {
    this.operations.set([{ command: 'SET', key: '', value: '' }]);
    this.result.set(null);
  }

  /**
   * Checks if a value is required for the operation command
   *
   * @param op - Operation to check
   * @returns True if value is required
   */
  isValueRequired(op: OperationDraft): boolean {
    return op.command === 'SET';
  }

  /**
   * Validates all operations in the transaction
   *
   * @returns Array of validation error messages (empty if valid)
   */
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

  /**
   * Executes the transaction with all operations
   * Validates operations before execution
   */
  execute(): void {
    const errors = this.validate();
    if (errors.length) {
      const firstError = errors[0];
      if (firstError) {
        this.snackBar.open(firstError, 'Close', { duration: 4000 });
      }
      return;
    }
    this.executing.set(true);
    this.result.set(null);

    const request: ExecuteTransactionRequest = {
      operations: this.operations().map<RedisOperation>(o => {
        const trimmedValue = o.value?.trim();
        return {
          command: o.command,
          key: o.key,
          value: trimmedValue || ''
        };
      })
    };

    this.executionPending.set(true);
    this.redisOperationsStore.executeTransaction({ connectionId: this.connectionId(), request });
  }
}
