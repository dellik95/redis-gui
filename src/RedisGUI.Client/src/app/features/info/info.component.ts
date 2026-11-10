import { Component, inject, ChangeDetectionStrategy, effect, signal, computed } from '@angular/core';
import { KeyValuePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { combineLatest } from 'rxjs';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { filter } from 'rxjs/operators';

import { ServerInfoStore } from '../../core/stores/server-info.store';
import { ConnectionsStore } from '../../core/stores/connections.store';
import { PromptDialogComponent, PageHeaderComponent } from '../../shared/components';
import { UiSelectComponent, UiSelectOption, UiInputComponent } from '../../shared/ui';

/**
 * Info Component
 * Displays Redis server information and configuration
 *
 * Features:
 * - Server information (version, memory, clients, etc.)
 * - Configuration parameters
 * - Config search functionality
 * - Config editing (for modifiable parameters)
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
    MatInputModule,
    MatFormFieldModule,
    MatDialogModule,
    MatTooltipModule,
    MatSelectModule,
    FormsModule,
    UiSelectComponent,
    UiInputComponent,
    PageHeaderComponent
  ],
  templateUrl: './info.component.html',
  styleUrl: './info.component.scss'
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
  readonly activeConnectionId = computed(() => {
    return this.userSelectedConnectionId() 
      ?? this.routeConnectionId() 
      ?? this.connectionsStore.connections()[0]?.id 
      ?? null;
  });

  // 5. Connections list
  readonly connections = computed(() => this.connectionsStore.connections());
  readonly connectionOptions = computed<UiSelectOption[]>(() => {
    return this.connections().map(c => ({
      label: `${c.name} (${c.host}:${c.port})`,
      value: c.id,
      icon: 'dns'
    }));
  });

  // 6. State signals
  protected readonly configSearchQuery = signal('');

  // 7. Configuration table columns
  readonly configColumns = ['name', 'value', 'modifiable', 'actions'] as const;

  constructor() {
    this.connectionsStore.loadConnections();

    // Watch for activeConnectionId changes and load server info/config
    effect(() => {
      const id = this.activeConnectionId();
      if (id) {
        this.loadData();
      }
    });
  }

  onConnectionChange(id: string): void {
    this.userSelectedConnectionId.set(id);
  }

  /**
   * Loads server info and configuration data
   */
  loadData(): void {
    const id = this.activeConnectionId();
    if (!id) return;

    this.store.loadServerInfo(id);
    this.store.loadConfig({ connectionId: id });
  }

  /**
   * Searches configuration parameters by pattern
   */
  searchConfig(): void {
    const id = this.activeConnectionId();
    if (!id) return;

    const query = this.configSearchQuery();
    this.store.loadConfig(query
      ? { connectionId: id, pattern: query }
      : { connectionId: id }
    );
  }

  /**
   * Edits a configuration parameter
   *
   * @param paramName - Name of the parameter to edit
   * @param currentValue - Current value of the parameter
   */
  editConfig(paramName: string, currentValue: string): void {
    const id = this.activeConnectionId();
    if (!id) return;

    this.dialog.open(PromptDialogComponent, {
      data: {
        title: 'Edit Configuration',
        message: `Enter new value for "${paramName}":`,
        placeholder: 'Value',
        defaultValue: currentValue,
        required: true
      }
    }).afterClosed()
      .pipe(filter((value): value is string => value !== null && value !== currentValue))
      .subscribe(newValue => {
        this.store.updateConfig({
          connectionId: id,
          request: { parameterName: paramName, parameterValue: newValue }
        });
      });
  }
}
