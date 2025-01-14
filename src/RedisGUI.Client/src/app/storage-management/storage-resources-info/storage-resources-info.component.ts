import { Component, inject, Input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Observable, switchMap } from 'rxjs';
import { StorageResourseDetailsService } from '../services/storage-resourse-details.service';
import { AsyncPipe } from '@angular/common';
import { PORTAL_DATA } from '../../../shared/services/section-actions-portal.service';
import { SystemMetrics } from "../types/system-metrics.type"

@Component({
  selector: 'app-storage-resources-info',
  imports: [MatIconModule, AsyncPipe],
  templateUrl: './storage-resources-info.component.html',
  styleUrl: './storage-resources-info.component.scss'
})
export class StorageResourcesInfoComponent {

  resourseDetailsService = inject(StorageResourseDetailsService);
  resourseInfo$: Observable<SystemMetrics> = new Observable<SystemMetrics>();
  private readonly data = inject<Map<string, string>>(PORTAL_DATA);


  ngOnInit() {
    let id = this.data.get("id") ?? "";
    this.resourseInfo$ = this.resourseDetailsService.startConnection(id).pipe(switchMap(() => {
      return this.resourseDetailsService.receiveMessage();
    }));
  }

  ngOnDestroy() {
    this.resourseDetailsService.stopConnection();
  }

}
