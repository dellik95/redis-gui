import { Component, input, ChangeDetectionStrategy } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

/**
 * Reusable empty / loading state block.
 * Covers three modes via `type`:
 *  - 'empty'   — icon + heading + description + optional action slot
 *  - 'loading' — spinner + optional message
 *
 * @example
 * ```html
 * <app-empty-state type="loading" message="Loading server information..." />
 *
 * <app-empty-state type="empty" icon="dns" heading="No Server Information Loaded"
 *   description="Select a Redis connection above.">
 *   <button class="btn-primary" (click)="load()">Load</button>
 * </app-empty-state>
 * ```
 */
@Component({
  selector: 'app-empty-state',
  standalone: true,
  imports: [MatIconModule, MatProgressSpinnerModule],
  templateUrl: './empty-state.component.html',
  styleUrl: './empty-state.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmptyStateComponent {
  readonly type = input<'empty' | 'loading'>('empty');
  readonly icon = input<string>('inbox');
  readonly heading = input<string>('');
  readonly description = input<string>('');
  readonly message = input<string>('');
  /** Spinner diameter for loading type */
  readonly spinnerDiameter = input<number>(44);
}
