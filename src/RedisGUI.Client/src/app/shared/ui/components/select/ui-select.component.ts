import {
  signal,
  ChangeDetectionStrategy,
  Component,
  computed,
  forwardRef,
  input,
  model,
  output
} from '@angular/core';
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
  imports: [FormsModule, MatMenuModule, MatIconModule, MatTooltipModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiSelectComponent),
      multi: true
    }
  ],
  templateUrl: './ui-select.component.html',
  styleUrl: './ui-select.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiSelectComponent implements FormValueControl<any>, ControlValueAccessor {
  readonly value = model<any>();
  readonly options = input<readonly UiSelectOption[]>([]);
  readonly label = input<string>();
  readonly placeholder = input<string>('Select...');
  readonly disabled = input<boolean>(false);
  private readonly formDisabled = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
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

  setDisabledState(isDisabled: boolean): void {
    this.formDisabled.set(isDisabled);
  }
}
