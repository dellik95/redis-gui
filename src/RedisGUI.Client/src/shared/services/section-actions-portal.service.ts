import { Injectable, ComponentRef } from '@angular/core';
import { ComponentPortal } from '@angular/cdk/portal';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class SectionActionsPortalService {
  private portalContent = new BehaviorSubject<ComponentPortal<any> | null>(null);
  portalContent$ = this.portalContent.asObservable();

  private activeComponentRef: ComponentRef<any> | null = null;

  constructor() { }

  /**
   * Attach a component to the portal
   * @param component Component to be rendered in the portal
   */
  attach<T>(component: ComponentPortal<T>): void {
    this.portalContent.next(component);
  }

  /**
   * Clear the current portal content
   */
  clear(): void {
    if (this.activeComponentRef) {
      this.activeComponentRef.destroy();
    }
    this.portalContent.next(null);
  }

  /**
   * Set reference to the currently active component
   * @param componentRef Reference to the active component
   */
  setActiveComponent(componentRef: ComponentRef<any>): void {
    this.activeComponentRef = componentRef;
  }
}
