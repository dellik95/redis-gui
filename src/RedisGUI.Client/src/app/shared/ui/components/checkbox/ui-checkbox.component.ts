import { ChangeDetectionStrategy, Component, forwardRef, input, model, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormCheckboxControl } from '@angular/forms/signals';

@Component({
  selector: 'ui-checkbox',
  standalone: true,
  imports: [CommonModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiCheckboxComponent),
      multi: true
    }
  ],
  template: `
    <label class="ui-checkbox-wrapper" [class.is-disabled]="disabled()">
      <div class="checkbox-box" [class.is-checked]="checked()">
        <input
          type="checkbox"
          [checked]="checked()"
          [disabled]="disabled()"
          (change)="onToggle($event)"
          class="hidden-input"
        />
        @if (checked()) {
          <svg class="check-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="20 6 9 17 4 12"></polyline>
          </svg>
        }
      </div>

      <div class="label-block">
        @if (label()) {
          <span class="checkbox-label">{{ label() }}</span>
        }
        @if (description()) {
          <span class="checkbox-desc">{{ description() }}</span>
        }
      </div>
    </label>
  `,
  styles: [`
    :host {
      display: inline-block;
      vertical-align: middle;
    }

    .ui-checkbox-wrapper {
      display: inline-flex;
      align-items: flex-start;
      gap: 10px;
      cursor: pointer;
      user-select: none;
      font-family: inherit;

      &.is-disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .checkbox-box {
      width: 18px;
      height: 18px;
      border-radius: 5px;
      border: 1.5px solid var(--color-border-light, #334155);
      background: var(--color-canvas-subtle, #0d1322);
      display: flex;
      align-items: center;
      justify-content: center;
      transition: all 0.15s ease-in-out;
      margin-top: 1px;
      flex-shrink: 0;

      &:hover:not(.is-disabled) {
        border-color: var(--color-accent, #6366f1);
      }

      &.is-checked {
        background: var(--color-accent, #6366f1);
        border-color: var(--color-accent, #6366f1);
        box-shadow: 0 0 8px var(--color-accent-glow, rgba(99, 102, 241, 0.4));
      }
    }

    .hidden-input {
      position: absolute;
      opacity: 0;
      width: 0;
      height: 0;
      margin: 0;
      pointer-events: none;
    }

    .check-icon {
      width: 12px;
      height: 12px;
      color: #ffffff;
    }

    .label-block {
      display: flex;
      flex-direction: column;
      gap: 2px;
    }

    .checkbox-label {
      font-size: 13px;
      font-weight: 500;
      color: var(--color-text, #f8fafc);
      line-height: 1.4;
    }

    .checkbox-desc {
      font-size: 11.5px;
      color: var(--color-text-muted, #94a3b8);
      line-height: 1.3;
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiCheckboxComponent implements FormCheckboxControl, ControlValueAccessor {
  readonly checked = model<boolean>(false);
  readonly label = input<string>();
  readonly description = input<string>();
  readonly disabled = input<boolean>(false);

  // Signal Forms & Control Integration
  readonly name = input<string>('');
  readonly invalid = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly any[]>([]);
  readonly touch = output<void>();

  private onChange: (val: boolean) => void = () => {};
  private onTouched: () => void = () => {};

  onToggle(event: Event): void {
    if (!this.disabled()) {
      const target = event.target as HTMLInputElement;
      const isChecked = target.checked;
      this.checked.set(isChecked);
      this.onChange(isChecked);
      this.onTouched();
      this.touch.emit();
    }
  }

  // ControlValueAccessor
  writeValue(val: any): void {
    this.checked.set(!!val);
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // Handled
  }
}
