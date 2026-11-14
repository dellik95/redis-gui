import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RedisDataType } from '../../../../../models/redis-keys.types';

@Component({
  selector: 'app-type-badge',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './type-badge.component.html',
  styleUrl: './type-badge.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TypeBadgeComponent {
  readonly type = input.required<RedisDataType | string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly showIcon = input<boolean>(true);

  private readonly normalizedType = computed(() => {
    const raw = String(this.type() || '')
      .toLowerCase()
      .trim();
    if (raw === 'sortedset' || raw === 'zset') return 'zset';
    return raw;
  });

  readonly badgeClass = computed(() => `type-${this.normalizedType() || 'unknown'}`);

  readonly displayLabel = computed(() => {
    const norm = this.normalizedType();
    switch (norm) {
      case 'string':
        return 'STRING';
      case 'list':
        return 'LIST';
      case 'set':
        return 'SET';
      case 'hash':
        return 'HASH';
      case 'zset':
        return 'ZSET';
      case 'stream':
        return 'STREAM';
      default:
        return (this.type() || 'UNKNOWN').toString().toUpperCase();
    }
  });

  readonly iconName = computed(() => {
    switch (this.normalizedType()) {
      case 'string':
        return 'text_fields';
      case 'list':
        return 'format_list_numbered';
      case 'set':
        return 'category';
      case 'hash':
        return 'data_object';
      case 'zset':
        return 'stacked_bar_chart';
      case 'stream':
        return 'water';
      default:
        return 'help_outline';
    }
  });
}
