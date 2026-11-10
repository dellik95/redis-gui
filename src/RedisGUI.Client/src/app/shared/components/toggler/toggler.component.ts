import { Component, input, output, model, ChangeDetectionStrategy } from '@angular/core';
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
    label?: string;
  }>;
};

/**
 * Modern Segmented Control / Toggler Component
 * Clean developer-first segmented control without Material MDC bloat
 */
@Component({
  selector: 'app-toggler',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatIconModule],
  templateUrl: './toggler.component.html',
  styleUrl: './toggler.component.scss'
})
export class TogglerComponent {
  // 1. Inputs & Models
  readonly config = input.required<TogglerConfig>();
  readonly value = model<string>();
  
  // 2. Outputs (backward compatibility)
  readonly toggleChanged = output<string>();

  // 3. Handlers
  selectItem(itemValue: string): void {
    this.value.set(itemValue);
    this.toggleChanged.emit(itemValue);
  }
}
