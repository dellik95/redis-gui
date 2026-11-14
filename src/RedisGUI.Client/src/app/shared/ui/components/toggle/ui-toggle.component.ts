import {
  Component,
  input,
  model,
  output,
  computed,
  ChangeDetectionStrategy
} from '@angular/core';

/**
 * Custom toggle/switch component that matches the application design system.
 * Replaces mat-slide-toggle with a fully styled, accessible alternative.
 *
 * Supports both two-way binding via model() and event-driven usage:
 *
 * @example
 * ```html
 * <!-- Two-way binding -->
 * <ui-toggle [(checked)]="autoRefresh" label="Auto-refresh" />
 *
 * <!-- Event-driven -->
 * <ui-toggle [checked]="isActive()" (checkedChange)="onToggle($event)" label="Active" />
 * ```
 */
@Component({
  selector: 'ui-toggle',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './ui-toggle.component.html',
  styleUrl: './ui-toggle.component.scss'
})
export class UiToggleComponent {
  readonly checked = model<boolean>(false);
  readonly label = input<string>('');
  readonly disabled = input<boolean>(false);

  onToggle(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.checked.set(input.checked);
  }
}
