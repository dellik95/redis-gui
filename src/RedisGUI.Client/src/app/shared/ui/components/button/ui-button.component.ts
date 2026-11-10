import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';

export type ButtonVariant =
  | 'primary'
  | 'secondary'
  | 'success'
  | 'warning'
  | 'danger'
  | 'ghost'
  | 'outline'
  | 'icon';

export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'ui-button',
  standalone: true,
  imports: [CommonModule, MatIconModule],
  template: `
    <button
      [type]="type()"
      [disabled]="isDisabled()"
      [class]="buttonClasses()"
      (click)="onClick($event)">
      @if (loading()) {
        <mat-icon class="animate-spin text-current !text-[18px] !w-[18px] !h-[18px] flex items-center justify-center !m-0 !p-0">autorenew</mat-icon>
      } @else if (icon() && iconPosition() === 'left') {
        <mat-icon class="text-current !text-[18px] !w-[18px] !h-[18px] flex items-center justify-center !m-0 !p-0">{{ icon() }}</mat-icon>
      }
      
      <ng-content></ng-content>

      @if (!loading() && icon() && iconPosition() === 'right') {
        <mat-icon class="text-current !text-[18px] !w-[18px] !h-[18px] flex items-center justify-center !m-0 !p-0">{{ icon() }}</mat-icon>
      }
    </button>
  `,
  styles: [`
    :host {
      display: inline-block;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiButtonComponent {
  readonly variant = input<ButtonVariant>('secondary');
  readonly size = input<ButtonSize>('md');
  readonly type = input<'button' | 'submit' | 'reset'>('button');
  readonly disabled = input(false);
  readonly loading = input(false);
  readonly icon = input<string>();
  readonly iconPosition = input<'left' | 'right'>('left');
  readonly fullWidth = input(false);

  readonly clicked = output<MouseEvent>();

  readonly isDisabled = computed(() => this.disabled() || this.loading());

  readonly buttonClasses = computed(() => {
    const base = 'inline-flex items-center justify-center gap-2 font-medium rounded-lg transition-all duration-150 select-none focus:outline-none';
    const width = this.fullWidth() ? 'w-full' : '';
    const state = this.isDisabled() ? 'opacity-50 cursor-not-allowed pointer-events-none' : 'cursor-pointer active:translate-y-0 hover:-translate-y-0.5';

    const sizes: Record<ButtonSize, string> = {
      sm: this.variant() === 'icon' ? 'w-7 h-7 p-0' : 'px-2.5 py-1 text-xs',
      md: this.variant() === 'icon' ? 'w-8 h-8 p-0' : 'px-3.5 py-1.5 text-xs',
      lg: this.variant() === 'icon' ? 'w-10 h-10 p-0' : 'px-5 py-2.5 text-sm font-semibold'
    };

    const variants: Record<ButtonVariant, string> = {
      primary: 'bg-[var(--color-accent)] text-white shadow-sm hover:bg-[var(--color-accent-hover)] shadow-[0_0_12px_var(--color-accent-glow)]',
      secondary: 'bg-[var(--color-card)] text-[var(--color-text)] border border-[var(--color-border)] hover:bg-[var(--color-card-hover)] hover:border-[var(--color-border-light)]',
      success: 'bg-emerald-500/15 text-emerald-400 border border-emerald-500/30 hover:bg-emerald-500/25',
      warning: 'bg-amber-500/15 text-amber-400 border border-amber-500/30 hover:bg-amber-500/25',
      danger: 'bg-rose-500/15 text-rose-400 border border-rose-500/30 hover:bg-rose-500/25',
      ghost: 'bg-transparent text-[var(--color-text-muted)] hover:bg-[var(--color-card-hover)] hover:text-[var(--color-text)]',
      outline: 'bg-transparent text-[var(--color-text)] border border-[var(--color-border)] hover:bg-[var(--color-card-hover)]',
      icon: 'bg-transparent text-[var(--color-text-muted)] hover:bg-[var(--color-card-hover)] hover:text-[var(--color-text)] rounded-full border border-[var(--color-border)]'
    };

    return `${base} ${sizes[this.size()]} ${variants[this.variant()]} ${width} ${state}`.trim();
  });

  onClick(event: MouseEvent): void {
    if (!this.isDisabled()) {
      this.clicked.emit(event);
    }
  }
}
