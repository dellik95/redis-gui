import { PortalModule, ComponentPortal } from '@angular/cdk/portal';
import { Component, inject, effect, signal, ChangeDetectionStrategy } from '@angular/core';
import { SectionActionsStore } from '../../../core/stores/section-actions.store';

/**
 * Section Actions Component
 * Portal container for dynamic section-specific action components
 * 
 * Allows different sections (dashboard, storage, etc.) to inject
 * custom action components into a shared location in the UI
 * 
 * @example
 * ```html
 * <app-section-actions />
 * ```
 */
@Component({
  selector: 'app-section-actions',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    PortalModule
  ],
  templateUrl: './section-actions.component.html',
  styleUrl: './section-actions.component.scss'
})
export class SectionActionsComponent {
  // 1. Injected dependencies
  private readonly sectionActionsStore = inject(SectionActionsStore);
  
  // 2. State signals
  /** Currently active portal component (or null if none) */
  readonly selectedPortal = signal<ComponentPortal<unknown> | null>(null);

  constructor() {
    effect(() => {
      this.selectedPortal.set(this.sectionActionsStore.getPortalContent());
    });
  }
}
