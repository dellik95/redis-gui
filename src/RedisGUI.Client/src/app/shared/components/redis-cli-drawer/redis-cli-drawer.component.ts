import {
  Component,
  signal,
  input,
  inject,
  ElementRef,
  viewChild,
  ChangeDetectionStrategy,
  DestroyRef,
  effect
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RedisCliService } from '../../../core/services/redis-cli.service';

export interface CliLogEntry {
  command: string;
  output: string;
  isError: boolean;
  timestamp: string;
  database: number;
}

@Component({
  selector: 'app-redis-cli-drawer',
  standalone: true,
  imports: [
        FormsModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './redis-cli-drawer.component.html',
  styleUrl: './redis-cli-drawer.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RedisCliDrawerComponent {
  private readonly cliService = inject(RedisCliService);
  private readonly destroyRef = inject(DestroyRef);

  readonly connectionId = input.required<string>();
  readonly activeDatabase = input<number>(0);
  readonly isOpen = signal<boolean>(false);
  readonly isMaximized = signal<boolean>(false);
  readonly isExecuting = signal<boolean>(false);

  readonly commandInput = signal<string>('');
  readonly history = signal<string[]>([]);
  readonly historyIndex = signal<number>(-1);
  readonly logs = signal<CliLogEntry[]>([]);

  readonly terminalOutput = viewChild<ElementRef<HTMLDivElement>>('terminalOutput');
  readonly cmdInputElement = viewChild<ElementRef<HTMLInputElement>>('cmdInput');

  readonly presets: { label: string; cmd: string }[] = [
    { label: 'PING', cmd: 'PING' },
    { label: 'INFO', cmd: 'INFO' },
    { label: 'DBSIZE', cmd: 'DBSIZE' },
    { label: 'CLIENT LIST', cmd: 'CLIENT LIST' },
    { label: 'SLOWLOG', cmd: 'SLOWLOG GET 10' },
    { label: 'MEMORY STATS', cmd: 'MEMORY STATS' },
    { label: 'TIME', cmd: 'TIME' }
  ];

  constructor() {
    effect(() => {
      if (this.isOpen()) {
        setTimeout(() => {
          this.cmdInputElement()?.nativeElement?.focus();
          this.scrollToBottom();
        }, 100);
      }
    });
  }

  toggleOpen(): void {
    this.isOpen.update(v => !v);
  }

  toggleMaximize(): void {
    this.isMaximized.update(v => !v);
  }

  executePreset(cmd: string): void {
    this.commandInput.set(cmd);
    this.runCommand();
  }

  runCommand(): void {
    const raw = this.commandInput().trim();
    if (!raw || this.isExecuting()) return;

    const connId = this.connectionId();
    if (!connId) return;

    // Add to command history
    this.history.update(h => [raw, ...h.filter(x => x !== raw)]);
    this.historyIndex.set(-1);

    this.isExecuting.set(true);
    const currentDb = this.activeDatabase();
    const timestamp = new Date().toLocaleTimeString();

    this.cliService
      .executeCommand(connId, raw, currentDb)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: res => {
          this.logs.update(prev => [
            ...prev,
            {
              command: raw,
              output: res.output,
              isError: !res.success,
              timestamp,
              database: currentDb
            }
          ]);
          this.commandInput.set('');
          this.isExecuting.set(false);
          this.scrollToBottom();
        },
        error: err => {
          const msg = err?.error?.message || err?.message || 'Execution failed';
          this.logs.update(prev => [
            ...prev,
            {
              command: raw,
              output: `(error) ERR ${msg}`,
              isError: true,
              timestamp,
              database: currentDb
            }
          ]);
          this.commandInput.set('');
          this.isExecuting.set(false);
          this.scrollToBottom();
        }
      });
  }

  navigateHistory(event: Event, direction: 'up' | 'down'): void {
    event.preventDefault();
    const list = this.history();
    if (list.length === 0) return;

    let idx = this.historyIndex();
    if (direction === 'up') {
      idx = Math.min(idx + 1, list.length - 1);
    } else {
      idx = Math.max(idx - 1, -1);
    }

    this.historyIndex.set(idx);
    if (idx >= 0 && idx < list.length) {
      const item = list[idx];
      if (item) this.commandInput.set(item);
    } else {
      this.commandInput.set('');
    }
  }

  clearTerminal(): void {
    this.logs.set([]);
  }

  copyAll(): void {
    const text = this.logs()
      .map(l => `[${l.timestamp}] db${l.database}> ${l.command}\n${l.output}`)
      .join('\n\n');
    navigator.clipboard.writeText(text);
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      const el = this.terminalOutput()?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    }, 50);
  }
}
