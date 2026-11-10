import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink } from '@angular/router';

import { BreadcrumbStore } from '../../../core/stores/breadcrumb.store';

/**
 * Component for displaying breadcrumb navigation
 * 
 * Features:
 * - Automatically updates based on route changes
 * - Uses BreadcrumbStore for centralized state management
 * - Displays navigation path with clickable links
 * 
 * @example
 * ```html
 * <app-breadcrumb />
 * ```
 */
@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './breadcrumb.component.html',
  styleUrl: './breadcrumb.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class BreadcrumbComponent {
  // 1. Injected dependencies
  private readonly breadcrumbStore = inject(BreadcrumbStore);
  
  // 2. Store signals
  /**
   * Current breadcrumb trail as signal from store
   */
  readonly breadcrumbs = this.breadcrumbStore.breadcrumbs;
}
