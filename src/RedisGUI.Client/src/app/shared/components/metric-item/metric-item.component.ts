import { Component, input, ChangeDetectionStrategy } from '@angular/core';

/**
 * Compact label + value pair used inside metric grids.
 * Used in connection-health-card (Latency, Failures, Last Checked).
 */
@Component({
  selector: 'app-metric-item',
  standalone: true,
  templateUrl: './metric-item.component.html',
  styleUrl: './metric-item.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MetricItemComponent {
  /** All-caps label above the value */
  readonly label = input.required<string>();
}
