import { Component, inject, signal, input, computed, ChangeDetectionStrategy, effect, DestroyRef } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { filter } from 'rxjs/operators';

import { RedisOperationsStore } from '../../../core/stores/redis-operations.store';
import { ExecuteLuaScriptRequest } from '../../../models/redis-advanced.types';
import { ConfirmDialogComponent, PromptDialogComponent } from '../../../shared/components';

/**
 * Saved Lua script with metadata
 */
interface SavedScript {
  id: string;
  name: string;
  script: string;
  lastRun?: Date;
  lastResult?: string;
  hash?: string;
}

/**
 * Lua Script Console Component
 * Interactive console for executing Lua scripts against Redis
 *
 * Features:
 * - Lua script editor
 * - Key and argument parameters
 * - Script execution with results
 * - Save/load scripts
 * - Script history
 *
 * @example
 * ```html
 * <app-lua-script-console [connectionId]="connectionId" />
 * ```
 */
@Component({
  selector: 'app-lua-script-console',
  standalone: true,
  templateUrl: './lua-script-console.component.html',
  styleUrl: './lua-script-console.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatTooltipModule
  ]
})
export class LuaScriptConsoleComponent {
  // 1. Injected dependencies
  private readonly redisOperationsStore = inject(RedisOperationsStore);
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);

  // 2. Inputs
  readonly connectionId = input.required<string>();

  // 3. State signals
  readonly scriptText = signal<string>("-- Example\nreturn 'Hello Redis'");
  readonly keys = signal<string[]>([]);
  readonly args = signal<string[]>([]);
  readonly executing = signal<boolean>(false);
  readonly result = signal<{ result: string; success: boolean; scriptHash?: string } | null>(null);
  readonly savedScripts = signal<SavedScript[]>(this.loadSavedScripts());
  readonly selectedScriptId = signal<string | null>(null);
  private readonly executionPending = signal<boolean>(false);

  // 4. Computed signals
  readonly canExecute = computed(() => {
    return this.scriptText().trim().length > 0 && !this.executing();
  });

  readonly selectedScript = computed(() => {
    const id = this.selectedScriptId();
    return id ? this.savedScripts().find(s => s.id === id) : null;
  });

  // 5. TrackBy functions for performance
  readonly trackByScriptId = (index: number, script: SavedScript) => script.id;
  readonly trackByIndex = (index: number) => index;

  // 6. Public methods - Keys management
  /**
   * Add a new key parameter
   */
  addKey(): void {
    this.keys.update(list => [...list, '']);
  }

  /**
   * Update a key parameter at the given index
   */
  updateKey(index: number, value: string): void {
    this.keys.update(list => {
      const copy = [...list];
      copy[index] = value;
      return copy;
    });
  }

  /**
   * Remove a key parameter at the given index
   */
  removeKey(index: number): void {
    this.keys.update(list => list.filter((_, i) => i !== index));
  }

  // 7. Public methods - Arguments management
  /**
   * Add a new argument parameter
   */
  addArg(): void {
    this.args.update(list => [...list, '']);
  }

  /**
   * Update an argument parameter at the given index
   */
  updateArg(index: number, value: string): void {
    this.args.update(list => {
      const copy = [...list];
      copy[index] = value;
      return copy;
    });
  }

  /**
   * Remove an argument parameter at the given index
   */
  removeArg(index: number): void {
    this.args.update(list => list.filter((_, i) => i !== index));
  }

  clearAll(): void {
    this.scriptText.set('');
    this.keys.set([]);
    this.args.set([]);
    this.result.set(null);
    this.selectedScriptId.set(null);
  }

  // 5. Constructor with effects
  constructor() {
    // Watch for script execution results
    effect(() => {
      if (!this.executionPending()) return;

      const resp = this.redisOperationsStore.lastScriptResult();
      if (resp) {
        this.result.set(resp);
        this.snackBar.open(resp.success ? 'Script executed' : 'Script failed', 'Close', { duration: 4000 });
        this.updateSavedScriptRun(resp.scriptHash, resp.result);
        this.executing.set(false);
        this.executionPending.set(false);
      }

      const err = this.redisOperationsStore.error();
      if (err) {
        this.snackBar.open(`Execution error: ${err}`, 'Close', { duration: 5000 });
        this.executing.set(false);
        this.executionPending.set(false);
      }
    });
  }

  /**
   * Execute the Lua script
   */
  execute(): void {
    const script = this.scriptText().trim();
    if (!script) {
      this.snackBar.open('Script required', 'Close', { duration: 3000 });
      return;
    }

    this.executing.set(true);
    this.result.set(null);

    const request: ExecuteLuaScriptRequest = {
      script,
      keys: this.keys().filter(k => k.trim() !== ''),
      arguments: this.args().filter(a => a.trim() !== '')
    };

    this.executionPending.set(true);
    this.redisOperationsStore.executeLuaScript({ connectionId: this.connectionId(), request });
  }

  saveScript(): void {
    this.dialog.open(PromptDialogComponent, {
      data: {
        title: 'Save Script',
        message: 'Enter a name for this script:',
        placeholder: 'Script name',
        required: true
      }
    }).afterClosed()
      .pipe(filter((name): name is string => name !== null && name.trim().length > 0))
      .subscribe(name => {
        const newScript: SavedScript = {
          id: crypto.randomUUID(),
          name: name.trim(),
          script: this.scriptText()
        };
        this.savedScripts.update(list => [...list, newScript]);
        this.persistSavedScripts();
        this.snackBar.open('Script saved', 'Close', { duration: 3000 });
      });
  }

  loadScript(id: string): void {
    const script = this.savedScripts().find(s => s.id === id);
    if (!script) return;
    this.scriptText.set(script.script);
    this.selectedScriptId.set(id);
    this.result.set(null);
  }

  deleteScript(id: string): void {
    const script = this.savedScripts().find(s => s.id === id);
    const scriptName = script?.name || 'this script';

    this.dialog.open(ConfirmDialogComponent, {
      data: {
        title: 'Delete Script',
        message: `Are you sure you want to delete "${scriptName}"?`,
        confirmText: 'Delete',
        cancelText: 'Cancel',
        confirmColor: 'warn'
      }
    }).afterClosed()
      .pipe(filter((confirmed): confirmed is boolean => confirmed === true))
      .subscribe(() => {
        this.savedScripts.update(list => list.filter(s => s.id !== id));
        if (this.selectedScriptId() === id) this.selectedScriptId.set(null);
        this.persistSavedScripts();
      });
  }

  private loadSavedScripts(): SavedScript[] {
    try {
      const raw = localStorage.getItem('luaScripts');
      if (!raw) return [];
      const parsed = JSON.parse(raw) as SavedScript[];
      return parsed.map(s => ({
        ...s,
        ...(s.lastRun ? { lastRun: new Date(s.lastRun) } : {})
      }));
    } catch { return []; }
  }

  private persistSavedScripts(): void {
    localStorage.setItem('luaScripts', JSON.stringify(this.savedScripts()));
  }

  private updateSavedScriptRun(hash: string | undefined, lastResult: string): void {
    if (!hash) return;
    // Optionally associate hash with selected script
    if (this.selectedScriptId()) {
      this.savedScripts.update(list => list.map(s => s.id === this.selectedScriptId() ? { ...s, hash, lastRun: new Date(), lastResult } : s));
      this.persistSavedScripts();
    }
  }
}
