import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { StorageConnectionsListComponent } from "./components/storage-connections-list/storage-connections-list.component";
import { SectionActionsPortalService } from '../../core/services/section-actions-portal.service';
import { ComponentPortal } from '@angular/cdk/portal';
import { DashboardSectionActionsComponent } from './components/dashboard-section-actions/dashboard-section-actions.component';
import { SectionConfig } from '../../models/section-config.type';
import { Connection } from '../../models/connection.type';
import { sectionConfig } from './dashboard-section.config';
import { SideNavService } from '../../shared/services/side-nav.service';

@Component({
  selector: 'app-dashboard',
  imports: [
    StorageConnectionsListComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {

  private sectionActionsPortalService = inject(SectionActionsPortalService);
  private sideNavService = inject(SideNavService);

  connectionListConfig: SectionConfig<Connection> = sectionConfig;

  ngOnInit(): void {
    // Clear connection-specific routes when returning to dashboard
    this.sideNavService.clearConnectionRoutes();
    this.showActions();
  }

  ngOnDestroy(): void {
    this.clearActions();
  }


  showActions() {
    const portal = new ComponentPortal(DashboardSectionActionsComponent);
    this.sectionActionsPortalService.attach(portal);
  }

  clearActions() {
    this.sectionActionsPortalService.clear();
  }

}
