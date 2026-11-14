import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export interface SparklineCurve {
  line: string;
  area: string;
  lastPoint: { x: number; y: number };
}

/**
 * Reusable metric card with an SVG sparkline chart.
 * Used in dashboard-metrics for Memory, CPU, Ops/sec, Traffic cards.
 */
@Component({
  selector: 'app-metric-sparkline-card',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './metric-sparkline-card.component.html',
  styleUrl: './metric-sparkline-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MetricSparklineCardComponent {
  /** Card header label */
  readonly label = input.required<string>();
  /** Material icon name */
  readonly icon = input.required<string>();
  /** Main displayed value (e.g. "128") */
  readonly primaryValue = input.required<string | number>();
  /** Unit suffix displayed small (e.g. "MB", "%") */
  readonly unit = input<string>('');
  /** Secondary info line below the value */
  readonly secondaryValue = input<string>('');
  /** Accent color hex used for stroke + gradient (e.g. "#38bdf8") */
  readonly color = input.required<string>();
  /** Unique SVG gradient id — must be unique per page instance */
  readonly gradientId = input.required<string>();
  /** Sparkline curve paths from generateSmoothCurve() */
  readonly curve = input.required<SparklineCurve>();
}
