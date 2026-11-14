import { Component, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltip } from '@angular/material/tooltip';
import {
  TogglerComponent,
  TogglerConfig
} from '../../../../shared/components/toggler/toggler.component';

@Component({
  selector: 'app-key-search-controls',
  imports: [MatIcon, MatMenuModule, MatTooltip, TogglerComponent],
  templateUrl: './key-search-controls.html',
  styleUrl: './key-search-controls.scss'
})
export class KeySearchControls {
  readonly selectedDb = input.required<number>();
  readonly availableDatabases = input.required<readonly { index: number; count: number }[]>();
  readonly keyDelimiter = input.required<string>();
  readonly searchPattern = input.required<string>();
  readonly databaseChange = output<number>();
  readonly modeChange = output<string>();
  readonly cycleDelimiter = output<void>();
  readonly searchChange = output<string>();
  readonly clear = output<void>();
  readonly palette = output<void>();
  readonly modesTogglerConfig: TogglerConfig = {
    defaultValue: 'table',
    items: [
      { value: 'table', icon: 'format_list_bulleted', label: 'Table' },
      { value: 'tree', icon: 'account_tree', label: 'Tree' }
    ]
  };
}
