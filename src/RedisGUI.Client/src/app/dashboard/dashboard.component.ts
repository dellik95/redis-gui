import { Component, inject, OnDestroy, OnInit } from '@angular/core';
import { RedisConnectionsListComponent as RedisConnectionsComponent } from "./redis-connections-list/redis-connections-list.component";
import { SectionActionsPortalService } from '../../shared/services/section-actions-portal.service';
import { ComponentPortal } from '@angular/cdk/portal';
import { DashboardSectionActionsComponent } from './dashboard-section-actions/dashboard-section-actions.component';
import { SectionConfigType } from '../../shared/types/section-config.type';
import { ConnectionType } from '../../shared/types/connection.type';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    RedisConnectionsComponent
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {

  private sectionActionsPortalService = inject(SectionActionsPortalService);

  connectionListConfig: SectionConfigType<ConnectionType> = [
    {
      name: "id",
      title: "Id",
      hidden: true
    },
    {
      name: "name",
      title: "Name",
    },
    {
      name: "host",
      title: "Address"
    },
    {
      name: "database",
      title: "Database number"
    }
  ];

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
