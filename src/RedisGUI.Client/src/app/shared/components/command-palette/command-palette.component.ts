import {
  Component,
  signal,
  computed,
  inject,
  ElementRef,
  viewChild,
  ChangeDetectionStrategy,
  AfterViewInit
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { RedisKeysStore } from '../../../core/stores/redis-keys.store';

export interface CommandItem {
  id: string;
  title: string;
  subtitle?: string;
  category: 'action' | 'database' | 'key';
  icon: string;
  badge?: string;
  action: () => void;
}

export interface CommandPaletteData {
  connectionId?: string;
  onOpenCli?: () => void;
  onAddNewKey?: () => void;
  onExport?: () => void;
  onImport?: () => void;
  onRefresh?: () => void;
  onSeedData?: () => void;
  onOpenSlowlog?: () => void;
  onOpenLoadTest?: () => void;
  onSelectKey?: (key: string) => void;
  onSwitchDb?: (db: number) => void;
}

@Component({
  selector: 'app-command-palette',
  standalone: true,
  imports: [FormsModule, MatDialogModule, MatIconModule],
  templateUrl: './command-palette.component.html',
  styleUrl: './command-palette.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class CommandPaletteComponent implements AfterViewInit {
  private readonly dialogRef = inject(MatDialogRef<CommandPaletteComponent>);
  private readonly data: CommandPaletteData = inject(MAT_DIALOG_DATA, { optional: true }) || {};
  private readonly redisKeysStore = inject(RedisKeysStore);

  readonly searchQuery = signal<string>('');
  readonly selectedIndex = signal<number>(0);
  readonly searchInput = viewChild<ElementRef<HTMLInputElement>>('searchInput');

  readonly allCommands = computed<CommandItem[]>(() => {
    const items: CommandItem[] = [];

    if (this.data.onAddNewKey) {
      items.push({
        id: 'action-add-key',
        title: 'Create New Key',
        subtitle: 'Add a new Redis key with type String, List, Set, Hash, ZSet, Stream',
        category: 'action',
        icon: 'add_circle',
        badge: 'Action',
        action: () => {
          this.close();
          this.data.onAddNewKey?.();
        }
      });
    }

    if (this.data.onOpenCli) {
      items.push({
        id: 'action-open-cli',
        title: 'Open Redis CLI Terminal',
        subtitle: 'Run raw Redis commands (INFO, DBSIZE, GET, SET, MONITOR)',
        category: 'action',
        icon: 'terminal',
        badge: 'CLI',
        action: () => {
          this.close();
          this.data.onOpenCli?.();
        }
      });
    }

    if (this.data.onSeedData) {
      items.push({
        id: 'action-seed',
        title: 'Seed Demo Data',
        subtitle: 'Populate sample keys across DB 0, 1, 2 (all types + varied TTLs)',
        category: 'action',
        icon: 'auto_awesome',
        badge: 'Seed',
        action: () => {
          this.close();
          this.data.onSeedData?.();
        }
      });
    }

    if (this.data.onOpenSlowlog) {
      items.push({
        id: 'action-slowlog',
        title: 'Inspect Redis Slowlog',
        subtitle: 'View slow commands and query execution profile',
        category: 'action',
        icon: 'history_toggle_off',
        badge: 'Slowlog',
        action: () => {
          this.close();
          this.data.onOpenSlowlog?.();
        }
      });
    }

    if (this.data.onOpenLoadTest) {
      items.push({
        id: 'action-load-test',
        title: 'Run Load Testing & Benchmark',
        subtitle: 'Generate synthetic traffic, measure latency & throughput with auto-cleanup',
        category: 'action',
        icon: 'speed',
        badge: 'Benchmark',
        action: () => {
          this.close();
          this.data.onOpenLoadTest?.();
        }
      });
    }

    if (this.data.onExport) {
      items.push({
        id: 'action-export',
        title: 'Export Redis Data',
        subtitle: 'Export keys to JSON or RESP format',
        category: 'action',
        icon: 'download',
        badge: 'Export',
        action: () => {
          this.close();
          this.data.onExport?.();
        }
      });
    }

    if (this.data.onImport) {
      items.push({
        id: 'action-import',
        title: 'Import Redis Data',
        subtitle: 'Import keys from backup JSON or RESP file',
        category: 'action',
        icon: 'upload',
        badge: 'Import',
        action: () => {
          this.close();
          this.data.onImport?.();
        }
      });
    }

    if (this.data.onRefresh) {
      items.push({
        id: 'action-refresh',
        title: 'Refresh Keys List',
        subtitle: 'Reload Redis keys and statistics',
        category: 'action',
        icon: 'refresh',
        badge: 'Reload',
        action: () => {
          this.close();
          this.data.onRefresh?.();
        }
      });
    }

    // Databases DB 0 - 15
    if (this.data.onSwitchDb) {
      const keyspace = this.redisKeysStore.keyspace();
      for (let db = 0; db <= 15; db++) {
        const count = keyspace[db] ?? 0;
        items.push({
          id: `db-${db}`,
          title: `Switch to Database ${db}`,
          subtitle: count > 0 ? `${count} active keys` : 'Empty database',
          category: 'database',
          icon: 'dns',
          badge: `DB ${db}`,
          action: () => {
            this.close();
            this.data.onSwitchDb?.(db);
          }
        });
      }
    }

    // Current loaded keys (top 30)
    const currentKeys = this.redisKeysStore.keys();
    for (const keyInfo of currentKeys.slice(0, 30)) {
      items.push({
        id: `key-${keyInfo.key}`,
        title: keyInfo.key,
        subtitle: `Type: ${keyInfo.type} | TTL: ${keyInfo.ttl !== null ? keyInfo.ttl + 's' : 'none'}`,
        category: 'key',
        icon: 'key',
        badge: keyInfo.type,
        action: () => {
          this.close();
          this.data.onSelectKey?.(keyInfo.key);
        }
      });
    }

    return items;
  });

  readonly filteredCommands = computed<CommandItem[]>(() => {
    const query = this.searchQuery().trim().toLowerCase();
    if (!query) {
      return this.allCommands();
    }
    return this.allCommands().filter(
      c =>
        c.title.toLowerCase().includes(query) ||
        (c.subtitle && c.subtitle.toLowerCase().includes(query)) ||
        (c.badge && c.badge.toLowerCase().includes(query))
    );
  });

  ngAfterViewInit(): void {
    setTimeout(() => {
      const input = this.searchInput()?.nativeElement;
      if (input) {
        input.focus({ preventScroll: true });
      }
    }, 60);
  }

  onKeyDown(event: KeyboardEvent): void {
    const list = this.filteredCommands();
    if (list.length === 0) return;

    if (event.key === 'ArrowDown') {
      event.preventDefault();
      this.selectedIndex.update(i => (i + 1) % list.length);
    } else if (event.key === 'ArrowUp') {
      event.preventDefault();
      this.selectedIndex.update(i => (i - 1 + list.length) % list.length);
    } else if (event.key === 'Enter') {
      event.preventDefault();
      const selected = list[this.selectedIndex()];
      if (selected) {
        selected.action();
      }
    } else if (event.key === 'Escape') {
      event.preventDefault();
      this.close();
    }
  }

  executeItem(cmd: CommandItem): void {
    cmd.action();
  }

  close(): void {
    this.dialogRef.close();
  }
}
