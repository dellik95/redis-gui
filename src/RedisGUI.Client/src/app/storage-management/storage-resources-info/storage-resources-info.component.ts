import { Component, inject } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { Observable, switchMap } from 'rxjs';
import { StorageResourseDetailsService } from '../services/storage-resourse-details.service';
import { AsyncPipe, DecimalPipe } from '@angular/common';
import { PORTAL_DATA } from '../../../shared/services/section-actions-portal.service';
import { SystemMetrics } from "../types/system-metrics.type"
import { FormatBytesPipe } from "../../../shared/pipes/format-bytes.pipe"

@Component({
  selector: 'app-storage-resources-info',
  imports: [MatIconModule, AsyncPipe, DecimalPipe, FormatBytesPipe],
  templateUrl: './storage-resources-info.component.html',
  styleUrl: './storage-resources-info.component.scss'
})
export class StorageResourcesInfoComponent {

  resourseDetailsService = inject(StorageResourseDetailsService);
  resourseInfo$: Observable<SystemMetrics> = new Observable<SystemMetrics>();
  private readonly data = inject<Map<string, string>>(PORTAL_DATA);


  ngOnInit() {
    let id = this.data.get("id") ?? "";
    this.resourseInfo$ = this.resourseDetailsService.startConnection(id).pipe(
      switchMap(() => {
        return this.resourseDetailsService.receiveMessage();
      }));
  }

  ngOnDestroy() {
    this.resourseDetailsService.stopConnection();
  }

}
