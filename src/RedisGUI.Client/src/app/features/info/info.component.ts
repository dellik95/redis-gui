import { Component, inject, DestroyRef, ChangeDetectionStrategy, effect, signal } from '@angular/core';
import { KeyValuePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { FormsModule } from '@angular/forms';

import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { filter } from 'rxjs/operators';

import { ServerInfoStore } from '../../core/stores/server-info.store';
import { PromptDialogComponent } from '../../shared/components';

/**
 * Info Component
 * Displays Redis server information and configuration
 *
 * Features:
 * - Server information (version, memory, clients, etc.)
 * - Configuration parameters
 * - Config search functionality
 * - Config editing (for modifiable parameters)
 *
 * @example
 * ```html
 * <app-info />
 * ```
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
    MatTabsModule,
    MatTableModule,
    MatIconModule,
    MatChipsModule,
    MatButtonModule,
    MatInputModule,
    MatFormFieldModule,
    MatDialogModule,
    MatTooltipModule,
    FormsModule
  ],
  templateUrl: './info.component.html',
  styleUrl: './info.component.scss'
})
export class InfoComponent {
  // 1. Injected dependencies
  private readonly route = inject(ActivatedRoute);
  protected readonly store = inject(ServerInfoStore);
  protected readonly dialog = inject(MatDialog);

  // 2. Inputs (from route)
  readonly connectionId = toSignal(
    this.route.paramMap.pipe(
      map(params => params.get('connectionId'))
    ),
    { initialValue: null }
  );

  // 3. State signals
  protected readonly configSearchQuery = signal('');

  // 4. Configuration
  /** Table columns for configuration display */
  readonly configColumns = ['name', 'value', 'modifiable', 'actions'] as const;

  // 5. Constructor with effects
  constructor() {
    // Watch for connectionId changes and load server info/config
    effect(() => {
      const id = this.connectionId();
      if (id) {
        this.loadData();
      }
    });
  }

  /**
   * Loads server info and configuration data
   */
  loadData(): void {
    const id = this.connectionId();
    if (!id) return;

    this.store.loadServerInfo(id);
    this.store.loadConfig({ connectionId: id });
  }

  /**
   * Searches configuration parameters by pattern
   */
  searchConfig(): void {
    const id = this.connectionId();
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
    const id = this.connectionId();
    if (!id) return;

    this.dialog.open(PromptDialogComponent, {
      data: {
        title: 'Edit Configuration',
        message: `Enter new value for ${paramName}:`,
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
