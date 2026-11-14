import {
  Component,
  inject,
  ChangeDetectionStrategy,
  effect,
  signal,
  computed,
  untracked
} from '@angular/core';
import { KeyValuePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { combineLatest, debounceTime, Subject } from 'rxjs';
import { filter } from 'rxjs/operators';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ServerInfoStore } from '../../core/stores/server-info.store';
import { ConnectionsStore } from '../../core/stores/connections.store';
import {
  PromptDialogComponent,
  PageHeaderComponent,
  InfoRowComponent,
  EmptyStateComponent
} from '../../shared/components';
import { UiSelectComponent, UiSelectOption, UiInputComponent } from '../../shared/ui';

/**
 * Info Component
 * Displays Redis server information and configuration.
 *
 * Config search is debounced (350 ms) so it fires automatically
 * while the user types — no separate Refresh button needed.
 */
@Component({
  selector: 'app-info',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    KeyValuePipe,
    DecimalPipe,
    MatCardModule,
    MatProgressSpinnerModule,
    MatProgressBarModule,
    MatTabsModule,
    MatTableModule,
    MatIconModule,
    MatChipsModule,
    MatButtonModule,
    MatTooltipModule,
    UiSelectComponent,
    UiInputComponent,
    PageHeaderComponent,
    InfoRowComponent,
    EmptyStateComponent
  ],
  templateUrl: './info.component.html',
  styleUrl: './info.component.scss',
  providers: [ServerInfoStore]
})
export class InfoComponent {
  // 1. Injected dependencies
  private readonly route = inject(ActivatedRoute);
  protected readonly store = inject(ServerInfoStore);
  protected readonly connectionsStore = inject(ConnectionsStore);
  protected readonly dialog = inject(MatDialog);

  // 2. Read connectionId from route param OR query param
  private readonly routeConnectionId = toSignal(
    combineLatest([this.route.paramMap, this.route.queryParamMap]).pipe(
      map(([params, queryParams]) => params.get('connectionId') ?? queryParams.get('connectionId'))
    ),
    { initialValue: null }
  );

  // 3. User-selected connection ID override
  readonly userSelectedConnectionId = signal<string | null>(null);

  // 4. Effective active connection ID
  readonly activeConnectionId = computed(
    () =>
      this.userSelectedConnectionId() ??
      this.routeConnectionId() ??
      this.connectionsStore.connections()[0]?.id ??
      null
  );

  // 5. Connection list for picker
  readonly connectionOptions = computed<UiSelectOption[]>(() =>
    this.connectionsStore.connections().map(c => ({
      label: `${c.name} (${c.host}:${c.port})`,
      value: c.id,
      icon: 'dns'
    }))
  );

  // 6. Config search — signal drives the UI; Subject drives the debounce
  readonly configSearchQuery = signal('');
  readonly selectedTab = signal(0);
  private readonly configSearch$ = new Subject<string>();

  // 7. Config table columns
  readonly configColumns = ['name', 'value', 'modifiable', 'actions'] as const;

  constructor() {
    this.connectionsStore.loadConnections();

    // Load data whenever the active connection changes
    effect(() => {
      const id = this.activeConnectionId();
      if (id) untracked(() => this.loadData());
    });

    effect(onCleanup => {
      const id = this.activeConnectionId();
      if (!id || this.selectedTab() !== 0) return;
      const timer = window.setInterval(() => {
        if (!this.store.isLoadingInfo() && !this.store.isPollingInfo()) {
          this.store.loadServerInfo({ connectionId: id, background: true });
        }
      }, 5000);
      onCleanup(() => window.clearInterval(timer));
    });

    // Debounced live config search (350 ms)
    this.configSearch$.pipe(debounceTime(350), takeUntilDestroyed()).subscribe(query => {
      const id = this.activeConnectionId();
      if (!id) return;
      this.loadConfig(id, query);
    });
  }

  onConnectionChange(id: string): void {
    this.userSelectedConnectionId.set(id);
  }

  /** Trigger from the (valueChange) event of the search input */
  onConfigSearch(query: string | number): void {
    const q = String(query ?? '');
    this.configSearchQuery.set(q);
    this.configSearch$.next(q);
  }

  /** Full reload — called by the header Refresh button */
  loadData(): void {
    const id = this.activeConnectionId();
    if (!id) return;
    this.store.loadServerInfo(id);
    this.loadConfig(id, this.configSearchQuery());
  }

  private loadConfig(connectionId: string, query: string): void {
    const term = query.trim();
    const pattern = term && !/[?*\[\]]/.test(term) ? `*${term}*` : term;
    this.store.loadConfig(pattern ? { connectionId, pattern } : { connectionId });
  }

  editConfig(paramName: string, currentValue: string): void {
    const id = this.activeConnectionId();
    if (!id) return;

    this.dialog
      .open(PromptDialogComponent, {
        data: {
          title: 'Edit Configuration',
          message: `Enter new value for "${paramName}":`,
          placeholder: 'Value',
          defaultValue: currentValue,
          required: true
        }
      })
      .afterClosed()
      .pipe(filter((value): value is string => value !== null && value !== currentValue))
      .subscribe(newValue => {
        this.store.updateConfig({
          connectionId: id,
          request: { parameterName: paramName, parameterValue: newValue }
        });
      });
  }
}
