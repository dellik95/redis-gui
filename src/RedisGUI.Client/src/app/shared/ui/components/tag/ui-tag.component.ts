import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export type UiTagVariant = 'default' | 'accent' | 'success' | 'warning' | 'danger' | 'string' | 'hash' | 'list' | 'set' | 'zset' | 'stream';

@Component({
  selector: 'ui-tag',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <span class="ui-tag" [class]="variantClass()" [class.size-sm]="size() === 'sm'">
      <span class="tag-label">{{ label() }}</span>
      @if (removable()) {
        <button type="button" class="tag-remove-btn" (click)="onRemove($event)" matTooltip="Remove">
          <mat-icon>close</mat-icon>
        </button>
      }
    </span>
  `,
  styles: [`
    :host {
      display: inline-block;
    }

    .ui-tag {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 3px 10px;
      border-radius: 6px;
      font-size: 12px;
      font-weight: 500;
      font-family: 'JetBrains Mono', 'Fira Code', monospace;
      border: 1px solid transparent;
      transition: all 0.15s ease;
      user-select: none;

      &.size-sm {
        padding: 2px 7px;
        font-size: 11px;
      }

      &.variant-default {
        background: var(--color-canvas, #090d16);
        color: var(--color-text, #f8fafc);
        border-color: var(--color-border, #1e293b);
      }

      &.variant-accent {
        background: rgba(99, 102, 241, 0.15);
        color: #a5b4fc;
        border-color: rgba(99, 102, 241, 0.35);
      }

      &.variant-success {
        background: rgba(16, 185, 129, 0.15);
        color: #6ee7b7;
        border-color: rgba(16, 185, 129, 0.35);
      }

      &.variant-warning {
        background: rgba(245, 158, 11, 0.15);
        color: #fcd34d;
        border-color: rgba(245, 158, 11, 0.35);
      }

      &.variant-danger {
        background: rgba(239, 68, 68, 0.15);
        color: #fca5a5;
        border-color: rgba(239, 68, 68, 0.35);
      }

      &.variant-set {
        background: rgba(168, 85, 247, 0.15);
        color: #d8b4fe;
        border-color: rgba(168, 85, 247, 0.35);
      }
    }

    .tag-label {
      line-height: 1.4;
    }

    .tag-remove-btn {
      background: transparent;
      border: none;
      padding: 0;
      margin: 0;
      display: flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: inherit;
      opacity: 0.6;
      border-radius: 50%;
      width: 14px;
      height: 14px;
      transition: opacity 0.15s ease;

      mat-icon {
        font-size: 12px;
        width: 12px;
        height: 12px;
      }

      &:hover {
        opacity: 1;
      }
    }
  `],
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
