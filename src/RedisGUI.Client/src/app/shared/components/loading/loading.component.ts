import { Component, input, inject, effect, ChangeDetectionStrategy } from '@angular/core';
import { NavigationCancel, NavigationEnd, NavigationError, NavigationStart, RouteConfigLoadEnd, RouteConfigLoadStart, Router } from "@angular/router";
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter } from 'rxjs/operators';

import { MatProgressSpinner } from '@angular/material/progress-spinner';

import { LoadingStore } from '../../../core/stores/loading.store';

/**
 * Loading indicator component
 * Shows a spinner during loading states with Angular 21 patterns
 * Optionally tracks router navigation events
 * Uses LoadingStore for centralized state management
 *
 * Features:
 * - Global loading state indicator
 * - Optional router navigation tracking
 * - Zoneless change detection compatible
 * - Signal-based state management
 *
 * @example
 * ```html
 * <loading [routing]="true" [detectRoutingOngoing]="true" />
 * ```
 */
@Component({
  selector: 'loading',
  standalone: true,
  templateUrl: './loading.component.html',
  styleUrl: './loading.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [MatProgressSpinner]
})
export class LoadingComponent {
  // 1. Injected dependencies
  readonly loadingStore = inject(LoadingStore);
  private readonly router = inject(Router);

  // 2. Inputs
  /**
   * Whether to show loading state for routing events
   */
  readonly routing = input<boolean>(false);

  /**
   * Whether to automatically detect and track routing events
   */
  readonly detectRoutingOngoing = input<boolean>(false);

  // 3. Constructor with effects
  constructor() {
    // Watch for detectRoutingOngoing changes and setup router tracking
    effect(() => {
      if (this.detectRoutingOngoing()) {
        this.setupRouterTracking();
      }
    });
  }

  /**
   * Sets up router event tracking for loading indicators
   * Uses takeUntilDestroyed() for automatic cleanup
   */
  private setupRouterTracking(): void {
    this.router.events
      .pipe(
        filter(event =>
          event instanceof NavigationStart ||
          event instanceof RouteConfigLoadStart ||
          event instanceof NavigationEnd ||
          event instanceof NavigationError ||
          event instanceof NavigationCancel ||
          event instanceof RouteConfigLoadEnd
        ),
        takeUntilDestroyed()
      )
      .subscribe(event => {
        if (event instanceof NavigationStart || event instanceof RouteConfigLoadStart) {
          this.loadingStore.loadingOn();
        } else if (
          event instanceof NavigationEnd ||
          event instanceof NavigationError ||
          event instanceof NavigationCancel ||
          event instanceof RouteConfigLoadEnd
        ) {
          this.loadingStore.loadingOff();
        }
      });
  }
}
