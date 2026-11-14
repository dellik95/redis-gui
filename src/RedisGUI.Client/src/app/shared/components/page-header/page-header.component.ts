import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

/**
 * Modern Page Header Component (Angular 22)
 * Unified top header for all pages featuring:
 * - Icon with styled accent badge
 * - Header title with optional status badge
 * - Informative subtitle
 * - Projected actions container ([actions]) for buttons, selectors, toggles
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './page-header.component.html',
  styleUrl: './page-header.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class PageHeaderComponent {
  readonly title = input.required<string>();
  readonly subtitle = input<string>();
  readonly icon = input<string>();
  readonly badge = input<string>();
  readonly badgeColor = input<'primary' | 'success' | 'warn' | 'accent'>('primary');
}
