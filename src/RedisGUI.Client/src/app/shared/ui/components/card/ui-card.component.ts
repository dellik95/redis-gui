import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type CardVariant = 'default' | 'elevated' | 'outlined';
export type CardPadding = 'none' | 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-card',
  standalone: true,
  imports: [],
  templateUrl: './ui-card.component.html',
  styleUrl: './ui-card.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiCardComponent {
  readonly variant = input<CardVariant>('default');
  readonly padding = input<CardPadding>('md');
  readonly hoverable = input(false);

  readonly cardClasses = computed(() => {
    const base =
      'rounded-xl transition-all duration-200 border border-[var(--color-border)] bg-[var(--color-card)] text-[var(--color-text)]';

    const paddings: Record<CardPadding, string> = {
      none: '',
      sm: 'p-3',
      md: 'p-4 sm:p-5',
      lg: 'p-6 sm:p-7'
    };

    const variants: Record<CardVariant, string> = {
      default: 'shadow-sm',
      elevated: 'shadow-lg shadow-black/40',
      outlined: 'shadow-none'
    };

    const hover = this.hoverable()
      ? 'hover:border-[var(--color-border-light)] hover:bg-[var(--color-card-hover)] hover:-translate-y-0.5 hover:shadow-md cursor-pointer'
      : '';

    return `${base} ${paddings[this.padding()]} ${variants[this.variant()]} ${hover}`.trim();
  });
}
