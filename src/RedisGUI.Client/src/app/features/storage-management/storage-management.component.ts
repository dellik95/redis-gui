import { ChangeDetectionStrategy, Component, inject, Injector, input, effect, signal } from '@angular/core';
import { StorageResourcesInfoComponent } from './components/storage-resources-info/storage-resources-info.component';
import { ComponentPortal } from '@angular/cdk/portal';
import { PORTAL_DATA, SectionActionsPortalService } from '../../core/services/section-actions-portal.service';
import { StorageValuesListComponent } from "./components/storage-values-list/storage-values-list.component";
import { InlineValueEditorComponent, ValueEditorData } from "./components/inline-value-editor/inline-value-editor.component";
import { ActivatedRoute } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { RedisKeysService } from '../../services/api/redis-keys.service';

@Component({
  selector: 'app-storage-management',
  imports: [StorageValuesListComponent, InlineValueEditorComponent],
  templateUrl: './storage-management.component.html',
  styleUrl: './storage-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorageManagementComponent {

  id = input<string>("");

  private sectionActionsPortalService = inject(SectionActionsPortalService);
  private readonly route = inject(ActivatedRoute);
  private readonly injector = inject(Injector);
  private readonly snackBar = inject(MatSnackBar);
  private readonly redisKeysService = inject(RedisKeysService);

  selectedKey = signal<ValueEditorData | null>(null);
  isSaving = signal<boolean>(false);

  constructor() {
    effect(() => {
      this.showStats();
    });
  }

  showStats() {
    let dataMap = new Map<string, string>([
      ["id", this.id()]
    ]);

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

  onKeySelected(keyData: ValueEditorData): void {
    this.selectedKey.set(keyData);
  }

  onSave(data: ValueEditorData): void {
    this.isSaving.set(true);
    this.redisKeysService.updateValue(this.id(), data.key, data.type, data.value, data.ttl)
      .subscribe({
        next: () => {
          this.snackBar.open("Key updated successfully", "Close", { duration: 3000 });
          this.selectedKey.set(null);
          this.isSaving.set(false);
        },
        error: (err: Error) => {
          this.snackBar.open(`Error updating key: ${err.message}`, "Close", { duration: 5000 });
          this.isSaving.set(false);
        }
      });
  }

  onCancel(): void {
    this.selectedKey.set(null);
  }

}
