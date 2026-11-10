import { ChangeDetectionStrategy, Component, computed, forwardRef, input, model, output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ControlValueAccessor, FormsModule, NG_VALUE_ACCESSOR } from '@angular/forms';
import { FormValueControl } from '@angular/forms/signals';
import { MatMenuModule } from '@angular/material/menu';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

export interface UiSelectOption<T = any> {
  label: string;
  value: T;
  icon?: string;
  badge?: string | number;
  disabled?: boolean;
}

@Component({
  selector: 'ui-select',
  standalone: true,
  imports: [CommonModule, FormsModule, MatMenuModule, MatIconModule, MatTooltipModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiSelectComponent),
      multi: true
    }
  ],
  template: `
    <div class="ui-select-wrapper" [class.full-width]="fullWidth()">
      @if (label()) {
        <label class="ui-select-label">
          {{ label() }}
          @if (required()) {
            <span class="required-asterisk">*</span>
          }
        </label>
      }

      <button
        type="button"
        [matMenuTriggerFor]="selectMenu"
        [disabled]="disabled()"
        class="select-trigger"
        [class.size-sm]="size() === 'sm'"
        [class.has-error]="!!computedError()"
        [class.is-disabled]="disabled()"
      >
        @if (selectedOption()?.icon || icon()) {
          <mat-icon class="prefix-icon">{{ selectedOption()?.icon || icon() }}</mat-icon>
        }

        <span class="trigger-label" [class.placeholder]="!hasSelection()">
          {{ selectedOption() ? selectedOption()!.label : (placeholder() || 'Select...') }}
        </span>

        <mat-icon class="chevron-icon">expand_more</mat-icon>
      </button>

      <mat-menu #selectMenu="matMenu" class="ui-select-dropdown-menu">
        @for (opt of options(); track opt.value) {
          <button
            mat-menu-item
            (click)="selectOption(opt.value)"
            [disabled]="opt.disabled"
            class="ui-select-item"
            [class.is-selected]="value() === opt.value"
          >
            @if (opt.icon) {
              <mat-icon class="item-icon">{{ opt.icon }}</mat-icon>
            }
            <span class="item-label">{{ opt.label }}</span>
            @if (opt.badge !== undefined && opt.badge !== null) {
              <span class="item-badge">{{ opt.badge }}</span>
            }
            @if (value() === opt.value) {
              <mat-icon class="check-icon">check</mat-icon>
            }
          </button>
        }
      </mat-menu>

      @if (computedError()) {
        <div class="select-error">
          <mat-icon class="error-icon">error_outline</mat-icon>
          <span>{{ computedError() }}</span>
        </div>
      }
    </div>
  `,
  styles: [`
    :host {
      display: inline-block;
      vertical-align: middle;
    }

    .ui-select-wrapper {
      display: flex;
      flex-direction: column;
      gap: 5px;
      font-family: inherit;

      &.full-width {
        width: 100%;
      }
    }

    .ui-select-label {
      font-size: 12px;
      font-weight: 600;
      color: var(--color-text-muted, #94a3b8);
      letter-spacing: 0.2px;

      .required-asterisk {
        color: #ef4444;
        margin-left: 2px;
      }
    }

    .select-trigger {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      background: var(--color-canvas-subtle, #0d1322);
      border: 1px solid var(--color-border, #1e293b);
      border-radius: 8px;
      height: 38px;
      padding: 0 12px;
      color: var(--color-text, #f8fafc);
      font-size: 13px;
      font-family: inherit;
      cursor: pointer;
      transition: all 0.15s ease-in-out;
      box-sizing: border-box;
      width: 100%;
      text-align: left;
      user-select: none;

      &:hover:not(.is-disabled) {
        background: var(--color-card-hover, #1e293b);
        border-color: var(--color-border-light, #334155);
      }

      &:focus-visible {
        outline: none;
        border-color: var(--color-accent, #6366f1);
        box-shadow: 0 0 0 3px var(--color-accent-glow, rgba(99, 102, 241, 0.25));
      }

      &.size-sm {
        height: 32px;
        padding: 0 10px;
        font-size: 12px;
        border-radius: 6px;

        .prefix-icon, .chevron-icon {
          font-size: 16px;
          width: 16px;
          height: 16px;
        }
      }

      &.has-error {
        border-color: #ef4444 !important;
      }

      &.is-disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }

    .prefix-icon {
      font-family: 'Material Icons', sans-serif !important;
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: var(--color-accent, #6366f1);
      flex-shrink: 0;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .trigger-label {
      flex: 1;
      min-width: 0;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: var(--color-text, #f8fafc);
      font-weight: 500;

      &.placeholder {
        color: var(--color-text-muted, #64748b);
        font-weight: 400;
      }
    }

    .chevron-icon {
      font-family: 'Material Icons', sans-serif !important;
      font-size: 18px;
      width: 18px;
      height: 18px;
      color: var(--color-text-muted, #94a3b8);
      flex-shrink: 0;
      margin-left: auto;
      display: flex;
      align-items: center;
      justify-content: center;
    }

    .select-error {
      display: flex;
      align-items: center;
      gap: 5px;
      font-size: 11.5px;
      color: #ef4444;

      .error-icon {
        font-size: 14px;
        width: 14px;
        height: 14px;
      }
    }
  `],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiSelectComponent implements FormValueControl<any>, ControlValueAccessor {
  readonly value = model<any>();
  readonly options = input<readonly UiSelectOption[]>([]);
  readonly label = input<string>();
  readonly placeholder = input<string>('Select...');
  readonly disabled = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly icon = input<string>();
  readonly size = input<'sm' | 'md'>('md');
  readonly error = input<string>();
  readonly fullWidth = input<boolean>(true);

  // Signal Forms & Control Integration
  readonly name = input<string>('');
  readonly invalid = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly any[]>([]);
  readonly touch = output<void>();

  private onChange: (val: any) => void = () => {};
  private onTouched: () => void = () => {};

  readonly computedError = computed(() => {
    if (this.error()) return this.error();
    if (this.touched() && this.invalid()) {
      const errs = this.errors();
      if (errs && errs.length > 0) {
        const first = errs[0];
        return first.message || (typeof first === 'string' ? first : 'Invalid field');
      }
    }
    return '';
  });

  readonly selectedOption = computed(() => {
    const val = this.value();
    return this.options().find(o => o.value === val);
  });

  readonly hasSelection = computed(() => {
    const val = this.value();
    return val !== null && val !== undefined && val !== '';
  });

  selectOption(optVal: any): void {
    this.value.set(optVal);
    this.onChange(optVal);
    this.onTouched();
    this.touch.emit();
  }

  // ControlValueAccessor implementation
  writeValue(val: any): void {
    this.value.set(val);
  }

  registerOnChange(fn: any): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: any): void {
    this.onTouched = fn;
  }

  setDisabledState?(isDisabled: boolean): void {
    // handled via input or reactive form
  }
}
