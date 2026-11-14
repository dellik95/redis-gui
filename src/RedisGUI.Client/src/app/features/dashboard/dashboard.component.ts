import { Component, inject, ChangeDetectionStrategy } from '@angular/core';
import { StorageConnectionsListComponent } from './components/storage-connections-list/storage-connections-list.component';
import { DashboardMetricsComponent } from './components/dashboard-metrics/dashboard-metrics.component';
import { PageHeaderComponent } from '../../shared/components';
import { SectionActionsStore } from '../../core/stores/section-actions.store';
import { SideNavStore } from '../../core/stores/side-nav.store';

/**
 * Dashboard component - Main landing page with live Redis metrics & connection management
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [StorageConnectionsListComponent, DashboardMetricsComponent, PageHeaderComponent],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class DashboardComponent {
  private readonly sectionActionsStore = inject(SectionActionsStore);
  private readonly sideNavStore = inject(SideNavStore);

  constructor() {
    this.sideNavStore.clearConnectionRoutes();
  }

  ngOnDestroy(): void {
    this.sectionActionsStore.clear();
  }
}
