import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { RedisDataType } from '../../../../../models/redis-keys.types';

@Component({
  selector: 'app-type-badge',
  standalone: true,
  imports: [MatIconModule],
  template: `
    <span
      class="type-badge"
      [class]="badgeClass()"
      [class.size-sm]="size() === 'sm'"
      [class.size-lg]="size() === 'lg'">
      @if (showIcon()) {
        <mat-icon class="type-icon">{{ iconName() }}</mat-icon>
      }
      <span class="type-text">{{ displayLabel() }}</span>
    </span>
  `,
  styles: [`
    .type-badge {
      display: inline-flex;
      align-items: center;
      gap: 4px;
      padding: 2px 8px;
      border-radius: 6px;
      font-size: 0.72rem;
      font-weight: 600;
      letter-spacing: 0.025em;
      text-transform: uppercase;
      line-height: 1.4;
      border: 1px solid transparent;
      user-select: none;
      transition: all 0.15s ease-in-out;

      &.size-sm {
        padding: 1px 6px;
        font-size: 0.65rem;
        gap: 2px;
        .type-icon {
          font-size: 12px;
          width: 12px;
          height: 12px;
        }
      }

      &.size-lg {
        padding: 4px 12px;
        font-size: 0.82rem;
        gap: 6px;
        .type-icon {
          font-size: 18px;
          width: 18px;
          height: 18px;
        }
      }

      .type-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
        display: flex;
        align-items: center;
        justify-content: center;
      }

      /* Type color variations */
      &.type-string {
        background-color: rgba(16, 185, 129, 0.12);
        color: #059669;
        border-color: rgba(16, 185, 129, 0.3);
      }

      &.type-list {
        background-color: rgba(14, 165, 233, 0.12);
        color: #0284c7;
        border-color: rgba(14, 165, 233, 0.3);
      }

      &.type-set {
        background-color: rgba(139, 92, 246, 0.12);
        color: #7c3aed;
        border-color: rgba(139, 92, 246, 0.3);
      }

      &.type-hash {
        background-color: rgba(245, 158, 11, 0.12);
        color: #d97706;
        border-color: rgba(245, 158, 11, 0.3);
      }

      &.type-zset, &.type-sortedset {
        background-color: rgba(244, 63, 94, 0.12);
        color: #e11d48;
        border-color: rgba(244, 63, 94, 0.3);
      }

      &.type-stream {
        background-color: rgba(99, 102, 241, 0.12);
        color: #4f46e5;
        border-color: rgba(99, 102, 241, 0.3);
      }

      &.type-unknown {
        background-color: rgba(100, 116, 139, 0.12);
        color: #64748b;
        border-color: rgba(100, 116, 139, 0.3);
      }
    }

    :host-context([data-theme="clean-light"]) .type-badge,
    :host-context(.light) .type-badge {
      font-weight: 700;
      &.type-string {
        background-color: rgba(16, 185, 129, 0.16);
        color: #047857;
        border-color: rgba(16, 185, 129, 0.45);
      }
      &.type-list {
        background-color: rgba(14, 165, 233, 0.16);
        color: #0369a1;
        border-color: rgba(14, 165, 233, 0.45);
      }
      &.type-set {
        background-color: rgba(139, 92, 246, 0.16);
        color: #6d28d9;
        border-color: rgba(139, 92, 246, 0.45);
      }
      &.type-hash {
        background-color: rgba(245, 158, 11, 0.16);
        color: #b45309;
        border-color: rgba(245, 158, 11, 0.45);
      }
      &.type-zset, &.type-sortedset {
        background-color: rgba(244, 63, 94, 0.16);
        color: #be123c;
        border-color: rgba(244, 63, 94, 0.45);
      }
      &.type-stream {
        background-color: rgba(99, 102, 241, 0.16);
        color: #4338ca;
        border-color: rgba(99, 102, 241, 0.45);
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TypeBadgeComponent {
  readonly type = input.required<RedisDataType | string>();
  readonly size = input<'sm' | 'md' | 'lg'>('md');
  readonly showIcon = input<boolean>(true);

  private readonly normalizedType = computed(() => {
    const raw = String(this.type() || '').toLowerCase().trim();
    if (raw === 'sortedset' || raw === 'zset') return 'zset';
    return raw;
  });

  readonly badgeClass = computed(() => `type-${this.normalizedType() || 'unknown'}`);

  readonly displayLabel = computed(() => {
    const norm = this.normalizedType();
    switch (norm) {
      case 'string': return 'STRING';
      case 'list': return 'LIST';
      case 'set': return 'SET';
      case 'hash': return 'HASH';
      case 'zset': return 'ZSET';
      case 'stream': return 'STREAM';
      default: return (this.type() || 'UNKNOWN').toString().toUpperCase();
    }
  });

  readonly iconName = computed(() => {
    switch (this.normalizedType()) {
      case 'string': return 'text_fields';
      case 'list': return 'format_list_numbered';
      case 'set': return 'category';
      case 'hash': return 'data_object';
      case 'zset': return 'stacked_bar_chart';
      case 'stream': return 'water';
      default: return 'help_outline';
    }
  });
}

