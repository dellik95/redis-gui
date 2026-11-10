import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant =
  | 'string'
  | 'hash'
  | 'list'
  | 'set'
  | 'zset'
  | 'stream'
  | 'healthy'
  | 'degraded'
  | 'unhealthy'
  | 'default'
  | 'accent';

@Component({
  selector: 'ui-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [class]="badgeClasses()">
      @if (dot()) {
        <span class="w-1.5 h-1.5 rounded-full bg-current"></span>
      }
      <ng-content></ng-content>
    </span>
  `,
  styles: [`
    :host {
      display: inline-flex;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiBadgeComponent {
  readonly variant = input<BadgeVariant | string>('default');
  readonly size = input<'sm' | 'md'>('md');
  readonly dot = input(false);

  readonly badgeClasses = computed(() => {
    const base = 'inline-flex items-center gap-1.5 font-semibold rounded-md uppercase tracking-wider select-none';
    const sizes = this.size() === 'sm' ? 'px-1.5 py-0.5 text-[10px]' : 'px-2 py-0.5 text-[11px]';

    const v = this.variant().toLowerCase();
    let variantClass = 'bg-slate-800 text-slate-300 border border-slate-700';

    switch (v) {
      case 'string':
        variantClass = 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30';
        break;
      case 'hash':
        variantClass = 'bg-amber-500/15 text-amber-400 border border-amber-500/30';
        break;
      case 'list':
        variantClass = 'bg-sky-500/15 text-sky-400 border border-sky-500/30';
        break;
      case 'set':
        variantClass = 'bg-purple-500/15 text-purple-400 border border-purple-500/30';
        break;
      case 'zset':
      case 'sortedset':
        variantClass = 'bg-pink-500/15 text-pink-400 border border-pink-500/30';
        break;
      case 'stream':
        variantClass = 'bg-indigo-500/15 text-indigo-400 border border-indigo-500/30';
        break;
      case 'healthy':
        variantClass = 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 shadow-[0_0_8px_rgba(16,185,129,0.2)]';
        break;
      case 'degraded':
        variantClass = 'bg-amber-500/15 text-amber-400 border border-amber-500/30';
        break;
      case 'unhealthy':
        variantClass = 'bg-rose-500/15 text-rose-400 border border-rose-500/30';
        break;
      case 'accent':
        variantClass = 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30';
        break;
    }

    return `${base} ${sizes} ${variantClass}`.trim();
  });
}
