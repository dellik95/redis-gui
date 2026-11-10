import { ChangeDetectionStrategy, Component, forwardRef, input, model, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormValueControl } from '@angular/forms/signals';

export interface UiRadioOption<T = any> {
  label: string;
  value: T;
  description?: string;
  icon?: string;
  disabled?: boolean;
}

@Component({
  selector: 'ui-radio-group',
  standalone: true,
  imports: [CommonModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiRadioGroupComponent),
      multi: true
    }
  ],
  template: `
    <div class="flex flex-col gap-2 select-none" [class.opacity-50]="disabled()" [class.pointer-events-none]="disabled()">
      @for (opt of options(); track opt.value) {
        <label
          (click)="selectOption(opt.value)"
          class="flex items-start gap-3 p-3 rounded-lg border cursor-pointer transition-all duration-150"
          [class.bg-[var(--color-accent)]/10]="value() === opt.value"
          [class.border-[var(--color-accent)]]="value() === opt.value"
          [class.border-[var(--color-border)]]="value() !== opt.value"
          [class.hover:border-[var(--color-border-light)]]="value() !== opt.value"
          [class.hover:bg-[var(--color-card-hover)]]="value() !== opt.value"
          [class.opacity-50]="opt.disabled"
          [class.pointer-events-none]="opt.disabled"
        >
          <!-- Radio circle indicator -->
          <div
            class="w-4 h-4 rounded-full border flex items-center justify-center mt-0.5 shrink-0 transition-colors"
            [class.border-[var(--color-accent)]]="value() === opt.value"
            [class.border-[var(--color-border-light)]]="value() !== opt.value"
          >
            @if (value() === opt.value) {
              <div class="w-2 h-2 rounded-full bg-[var(--color-accent)]"></div>
            }
          </div>

          <div class="flex flex-col gap-0.5">
            <span
              class="text-xs font-semibold"
              [class.text-[var(--color-accent)]]="value() === opt.value"
              [class.text-[var(--color-text)]]="value() !== opt.value"
            >
              {{ opt.label }}
            </span>
            @if (opt.description) {
              <span class="text-[11px] text-[var(--color-text-muted)] leading-tight">
                {{ opt.description }}
              </span>
            }
          </div>
        </label>
      }
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiRadioGroupComponent<T = any> implements FormValueControl<T>, ControlValueAccessor {
  readonly value = model<T>(null as unknown as T);
  readonly options = input<readonly UiRadioOption<T>[]>([]);
  readonly disabled = input<boolean>(false);
  readonly name = input<string>('');

  readonly invalid = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly any[]>([]);
  readonly touch = output<void>();

  private onChange: (val: T) => void = () => {};
  private onTouched: () => void = () => {};

  selectOption(optVal: T): void {
    if (this.disabled()) return;
    this.value.set(optVal);
    this.onChange(optVal);
    this.onTouched();
    this.touch.emit();
  }

  // CVA
  writeValue(val: T): void {
    this.value.set(val);
  }

  registerOnChange(fn: (val: T) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // Handled via input signal
  }
}
