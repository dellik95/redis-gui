import { ChangeDetectionStrategy, Component, inject, Injector, input } from '@angular/core';
import { StorageResourcesInfoComponent } from './storage-resources-info/storage-resources-info.component';
import { ComponentPortal } from '@angular/cdk/portal';
import { PORTAL_DATA, SectionActionsPortalService } from '../../shared/services/section-actions-portal.service';
import { StorageValuesListComponent } from "./storage-values-list/storage-values-list.component";
import { StorageValueEditorComponent } from "./storage-value-editor/storage-value-editor.component";
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-storage-management',
  imports: [StorageValuesListComponent, StorageValueEditorComponent],
  templateUrl: './storage-management.component.html',
  styleUrl: './storage-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageManagementComponent {

  id = input<string>("");

  private sectionActionsPortalService = inject(SectionActionsPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly injector = inject(Injector);

  ngOnInit(): void {
    this.showStats();
  }

  ngOnDestroy(): void {
    this.clearStat();
  }

  showStats() {
    let dataMap = new Map<string, string>([
      ["id", this.id()]
    ]);
    console.log(dataMap);

    let childInjector = this.createInjector<Map<string, string>>(dataMap);
    const portal = new ComponentPortal(StorageResourcesInfoComponent, null, childInjector);
    this.sectionActionsPortalService.attach(portal);
  }

  clearStat() {
    this.sectionActionsPortalService.clear();
  }

  private createInjector<T>(data: T): Injector {

    return Injector.create({
      providers: [
        {
          provide: PORTAL_DATA, useValue: data
        }
      ]
    });
  }

}
