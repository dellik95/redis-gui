import { PortalModule, ComponentPortal } from '@angular/cdk/portal';
import { Component, OnDestroy, OnInit } from '@angular/core';
import { SectionActionsPortalService } from '../../services/section-actions-portal.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-section-actions',
  standalone: true,
  imports: [
    PortalModule
  ],
  templateUrl: './section-actions.component.html',
  styleUrl: './section-actions.component.scss'
})
export class SectionActionsComponent implements OnInit, OnDestroy {
  selectedPortal: ComponentPortal<any> | null = null;
  private subscription: Subscription | null = null;

  constructor(private sectionActionsPortalService: SectionActionsPortalService) {}

  ngOnInit() {
    this.subscription = this.sectionActionsPortalService.portalContent$.subscribe(
      portal => {
        this.selectedPortal = portal;
      }
    );
  }

  ngOnDestroy() {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
