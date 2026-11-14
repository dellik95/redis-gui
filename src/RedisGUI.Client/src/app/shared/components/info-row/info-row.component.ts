import { Component, input, ChangeDetectionStrategy } from '@angular/core';

/**
 * Reusable label-value row for info cards.
 * Renders a full-width flex row with muted label on the left and
 * a bottom border separator. The value is projected via ng-content
 * so callers can pass pipes, chips, conditional classes, etc.
 *
 * @example
 * ```html
 * <app-info-row label="Redis Version">{{ store.serverInfo()!.redisVersion }}</app-info-row>
 * <app-info-row label="Mode"><mat-chip>{{ store.serverInfo()!.redisMode }}</mat-chip></app-info-row>
 * ```
 */
@Component({
  selector: 'app-info-row',
  standalone: true,
  templateUrl: './info-row.component.html',
  styleUrl: './info-row.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class InfoRowComponent {
  readonly label = input.required<string>();
}
