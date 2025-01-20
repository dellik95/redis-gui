import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { StorageConnectionsListComponent } from "./storage-connections-list/storage-connections-list.component";
import { SectionActionsPortalService } from '../../shared/services/section-actions-portal.service';
import { ComponentPortal } from '@angular/cdk/portal';
import { DashboardSectionActionsComponent } from './dashboard-section-actions/dashboard-section-actions.component';
import { SectionConfig } from '../../shared/types/section-config.type';
import { Connection } from '../../shared/types/connection.type';
import { sectionConfig } from './dashboard-section.config';

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

  connectionListConfig: SectionConfig<Connection> = sectionConfig;

  ngOnInit(): void {
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
