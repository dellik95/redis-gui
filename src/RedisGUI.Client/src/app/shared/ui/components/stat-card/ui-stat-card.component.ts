import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export type StatVariant = 'default' | 'primary' | 'healthy' | 'warning' | 'danger' | 'info';

@Component({
  selector: 'ui-stat-card',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <div class="stat-card-box">
      @if (icon()) {
        <div [class]="iconWrapperClasses()">
          <mat-icon class="stat-icon">{{ icon() }}</mat-icon>
        </div>
      }
      <div class="stat-details">
        <div class="stat-value">{{ value() }}</div>
        <div class="stat-label">{{ label() }}</div>
        @if (subtext()) {
          <div class="stat-subtext">{{ subtext() }}</div>
        }
      </div>
    </div>
  `,
  styles: [`
    :host {
      display: block;
    }
    .stat-card-box {
      display: flex;
      align-items: center;
      gap: 16px;
      padding: 16px 18px;
      background: var(--color-card, #0f172a);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 12px;
      transition: transform 0.15s ease, border-color 0.15s ease, box-shadow 0.15s ease;

      &:hover {
        transform: translateY(-2px);
        border-color: var(--color-border-light, #334155);
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.25);
      }
    }
    .stat-details {
      display: flex;
      flex-direction: column;
      min-width: 0;
    }
    .stat-value {
      font-size: 24px;
      font-weight: 700;
      color: var(--color-text, #f8fafc);
      line-height: 1.2;
    }
    .stat-label {
      font-size: 11.5px;
      font-weight: 500;
      color: var(--color-text-muted, #94a3b8);
      text-transform: uppercase;
      letter-spacing: 0.5px;
      margin-top: 2px;
    }
    .stat-subtext {
      font-size: 11px;
      color: var(--color-text-muted, #94a3b8);
      margin-top: 2px;
    }
    .stat-icon {
      font-size: 22px;
      width: 22px;
      height: 22px;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiStatCardComponent {
  readonly label = input.required<string>();
  readonly value = input.required<string | number>();
  readonly icon = input<string>();
  readonly subtext = input<string>();
  readonly variant = input<StatVariant>('default');

  readonly iconWrapperClasses = computed(() => {
    const base = 'w-11 h-11 rounded-xl flex items-center justify-center shrink-0';
    switch (this.variant()) {
      case 'primary':
        return `${base} bg-[var(--color-accent)]/15 text-[var(--color-accent)]`;
      case 'healthy':
        return `${base} bg-emerald-500/15 text-emerald-400`;
      case 'warning':
        return `${base} bg-amber-500/15 text-amber-400`;
      case 'danger':
        return `${base} bg-rose-500/15 text-rose-400`;
      case 'info':
        return `${base} bg-sky-500/15 text-sky-400`;
      default:
        return `${base} bg-slate-800 text-slate-300`;
    }
  });
}
