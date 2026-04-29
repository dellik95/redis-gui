import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { StorageConnectionsListComponent } from "./components/storage-connections-list/storage-connections-list.component";
import { SectionActionsStore } from '../../core/stores/section-actions.store';
import { ComponentPortal } from '@angular/cdk/portal';
import { DashboardSectionActionsComponent } from './components/dashboard-section-actions/dashboard-section-actions.component';
import { SectionConfig } from '../../models/section-config.type';
import { Connection } from '../../models/connection.type';
import { sectionConfig } from './dashboard-section.config';
import { SideNavStore } from '../../core/stores/side-nav.store';

/**
 * Dashboard component - Main landing page
 *
 * Displays Redis connection list and manages dashboard-specific section actions.
 * Uses Angular's automatic cleanup (no DestroyRef needed).
 * Modern zoneless-compatible implementation.
 */
@Component({
  selector: 'app-dashboard',
  imports: [StorageConnectionsListComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  // 1. Injected dependencies
  private readonly sectionActionsStore = inject(SectionActionsStore);
  private readonly sideNavStore = inject(SideNavStore);

  // 2. Configuration
  readonly connectionListConfig: SectionConfig<Connection> = sectionConfig;

  constructor() {
    // Clear connection-specific routes when returning to dashboard
    this.sideNavStore.clearConnectionRoutes();

    // Attach dashboard section actions (automatically cleaned up on destroy)
    const portal = new ComponentPortal(DashboardSectionActionsComponent);
    this.sectionActionsStore.attach(portal);
  }

  // ngOnDestroy is automatically called, clearing the portal
  ngOnDestroy(): void {
    this.sectionActionsStore.clear();
  }
}
