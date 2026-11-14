import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export type StatSummaryVariant = 'total' | 'healthy' | 'degraded' | 'unhealthy' | 'unknown';

/**
 * Horizontal stat summary card with an icon badge and a numeric value.
 * Used in health-monitoring summary grid.
 */
@Component({
  selector: 'app-stat-summary-card',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './stat-summary-card.component.html',
  styleUrl: './stat-summary-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatSummaryCardComponent {
  /** Color variant — drives icon background and color */
  readonly variant = input.required<StatSummaryVariant>();
  /** Material icon name */
  readonly icon = input.required<string>();
  /** Numeric or string value to display */
  readonly value = input.required<string | number>();
  /** Label text below the value */
  readonly label = input.required<string>();
}
