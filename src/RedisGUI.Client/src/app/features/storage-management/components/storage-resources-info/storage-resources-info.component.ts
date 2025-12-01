import { Component, inject, signal, effect } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { StorageResourseDetailsService } from '../../services/storage-resourse-details.service';
import { PORTAL_DATA } from '../../../../core/services/section-actions-portal.service';
import { SystemMetrics } from "../../models/system-metrics.type";
import { FormatBytesPipe } from "../../../../shared/pipes/format-bytes.pipe";
import { DecimalPipe } from '@angular/common';

@Component({
  selector: 'app-storage-resources-info',
  standalone: true,
  imports: [MatIconModule, FormatBytesPipe, DecimalPipe],
  templateUrl: './storage-resources-info.component.html',
  styleUrls: ['./storage-resources-info.component.scss']
})
export class StorageResourcesInfoComponent {

  resourseDetailsService = inject(StorageResourseDetailsService);
  resourseInfo = signal<SystemMetrics | null>(null);
  private readonly data = inject<Map<string, string>>(PORTAL_DATA);

  constructor() {
    effect(() => {
      let id = this.data.get("id") ?? "";
      this.resourseDetailsService.startConnection(id).subscribe(() => {
        this.resourseDetailsService.receiveMessage().subscribe((metrics: SystemMetrics) => {
          this.resourseInfo.set(metrics);
        });
      });
    });
  }

}
