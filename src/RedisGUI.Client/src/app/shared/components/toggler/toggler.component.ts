import { Component, input, output, ChangeDetectionStrategy } from '@angular/core';
import { MatButtonToggleChange, MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';

/**
 * Configuration for the toggler component
 */
export type TogglerConfig = {
  /** Default selected value */
  defaultValue: string | null;
  /** Array of toggle items with value and icon */
  items: Array<{
    value: string;
    icon: string;
  }>;
};

/**
 * Toggler Component
 * Material button toggle group with icon support
 * 
 * Features:
 * - Icon-based toggle buttons
 * - Configurable default value
 * - Emits value changes
 * 
 * @example
 * ```html
 * <app-toggler
 *   [config]="{ defaultValue: 'list', items: [{ value: 'list', icon: 'list' }, { value: 'grid', icon: 'grid_on' }] }"
 *   (toggleChanged)="onViewChange($event)"
 * />
 * ```
 */
@Component({
  selector: 'app-toggler',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatButtonToggleModule, MatIconModule],
  templateUrl: './toggler.component.html',
  styleUrl: './toggler.component.scss'
})
export class TogglerComponent {
  // 1. Inputs
  /** Toggler configuration with items and default value */
  readonly config = input.required<TogglerConfig>();
  
  // 2. Outputs
  /** Emitted when toggle selection changes */
  readonly toggleChanged = output<string>();

  // 3. Public methods
  /**
   * Handles toggle change event
   * 
   * @param event - Material button toggle change event
   */
  onToggle(event: MatButtonToggleChange): void {
    this.toggleChanged.emit(event.value);
  }
}
