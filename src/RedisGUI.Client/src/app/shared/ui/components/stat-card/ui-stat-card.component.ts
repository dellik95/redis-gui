import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

export type StatVariant = 'default' | 'primary' | 'healthy' | 'warning' | 'danger' | 'info';

@Component({
  selector: 'ui-stat-card',
  standalone: true,
  imports: [MatIconModule],
  templateUrl: './ui-stat-card.component.html',
  styleUrl: './ui-stat-card.component.scss',
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
