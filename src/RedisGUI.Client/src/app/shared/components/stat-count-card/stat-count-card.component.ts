import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * Vertical centre-aligned stat card with an icon, a large value and a label.
 * Used in audit-logs stats summary row.
 */
@Component({
  selector: 'app-stat-count-card',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './stat-count-card.component.html',
  styleUrl: './stat-count-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StatCountCardComponent {
  /** Material icon name */
  readonly icon = input.required<string>();
  /** Large displayed value */
  readonly value = input.required<string | number>();
  /** Small label below the value */
  readonly label = input.required<string>();
}
