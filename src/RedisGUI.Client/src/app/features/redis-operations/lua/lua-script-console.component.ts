import { Component, inject, signal, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { RedisOperationsService } from '../../../services/api/redis-operations.service';
import { ExecuteLuaScriptRequest } from '../../../models/redis-advanced.types';

interface SavedScript { id: string; name: string; script: string; lastRun?: Date; lastResult?: string; hash?: string; }

@Component({
  selector: 'app-lua-script-console',
  standalone: true,
  templateUrl: './lua-script-console.component.html',
  styleUrls: ['./lua-script-console.component.scss'],
  imports: [
    CommonModule,
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
  private redisOperationsService = inject(RedisOperationsService);
  private snackBar = inject(MatSnackBar);

  connectionId = input.required<string>();

  // Script editing state
  scriptText = signal<string>("-- Example\nreturn 'Hello Redis'");
  keys = signal<string[]>([]); // KEYS
  args = signal<string[]>([]); // ARGV

  executing = signal<boolean>(false);
  result = signal<{ result: string; success: boolean; scriptHash?: string } | null>(null);

  savedScripts = signal<SavedScript[]>(this.loadSavedScripts());
  selectedScriptId = signal<string | null>(null);

  addKey(): void { this.keys.update(list => [...list, '']); }
  updateKey(index: number, value: string): void { this.keys.update(list => { const c=[...list]; c[index]=value; return c; }); }
  removeKey(index: number): void { this.keys.update(list => list.filter((_,i)=>i!==index)); }

  addArg(): void { this.args.update(list => [...list, '']); }
  updateArg(index: number, value: string): void { this.args.update(list => { const c=[...list]; c[index]=value; return c; }); }
  removeArg(index: number): void { this.args.update(list => list.filter((_,i)=>i!==index)); }

  clearAll(): void {
    this.scriptText.set('');
    this.keys.set([]);
    this.args.set([]);
    this.result.set(null);
    this.selectedScriptId.set(null);
  }

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

    this.redisOperationsService.executeLuaScript(this.connectionId(), request).subscribe({
      next: resp => {
        this.result.set(resp);
        this.snackBar.open(resp.success ? 'Script executed' : 'Script failed', 'Close', { duration: 4000 });
        this.updateSavedScriptRun(resp.scriptHash, resp.result);
      },
      error: err => {
        this.snackBar.open(`Execution error: ${err.message}`, 'Close', { duration: 5000 });
      },
      complete: () => this.executing.set(false)
    });
  }

  saveScript(): void {
    const name = prompt('Enter script name');
    if (!name) return;
    const newScript: SavedScript = {
      id: crypto.randomUUID(),
      name: name.trim(),
      script: this.scriptText(),
      lastRun: undefined
    };
    this.savedScripts.update(list => [...list, newScript]);
    this.persistSavedScripts();
    this.snackBar.open('Script saved', 'Close', { duration: 3000 });
  }

  loadScript(id: string): void {
    const script = this.savedScripts().find(s => s.id === id);
    if (!script) return;
    this.scriptText.set(script.script);
    this.selectedScriptId.set(id);
    this.result.set(null);
  }

  deleteScript(id: string): void {
    if (!confirm('Delete script?')) return;
    this.savedScripts.update(list => list.filter(s => s.id !== id));
    if (this.selectedScriptId() === id) this.selectedScriptId.set(null);
    this.persistSavedScripts();
  }

  private loadSavedScripts(): SavedScript[] {
    try {
      const raw = localStorage.getItem('luaScripts');
      if (!raw) return [];
      const parsed = JSON.parse(raw) as SavedScript[];
      return parsed.map(s => ({ ...s, lastRun: s.lastRun ? new Date(s.lastRun) : undefined }));
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
