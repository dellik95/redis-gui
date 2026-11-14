import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export type UiTagVariant =
  | 'default'
  | 'accent'
  | 'success'
  | 'warning'
  | 'danger'
  | 'string'
  | 'hash'
  | 'list'
  | 'set'
  | 'zset'
  | 'stream';

@Component({
  selector: 'ui-tag',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './ui-tag.component.html',
  styleUrl: './ui-tag.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiTagComponent {
  readonly label = input<string>('');
  readonly removable = input<boolean>(false);
  readonly variant = input<UiTagVariant>('default');
  readonly size = input<'sm' | 'md'>('md');

  readonly removed = output<void>();

  readonly variantClass = computed(() => `variant-${this.variant()}`);

  onRemove(event: MouseEvent): void {
    event.stopPropagation();
    this.removed.emit();
  }
}
