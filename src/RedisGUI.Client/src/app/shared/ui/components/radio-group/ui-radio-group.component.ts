import {
  computed,
  signal,
  ChangeDetectionStrategy,
  Component,
  forwardRef,
  input,
  model,
  output
} from '@angular/core';
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
  imports: [],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiRadioGroupComponent),
      multi: true
    }
  ],
  templateUrl: './ui-radio-group.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiRadioGroupComponent<T = any> implements FormValueControl<T>, ControlValueAccessor {
  readonly value = model<T>(null as unknown as T);
  readonly options = input<readonly UiRadioOption<T>[]>([]);
  readonly disabled = input<boolean>(false);
  private readonly formDisabled = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  readonly name = input<string>('');

  readonly invalid = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly any[]>([]);
  readonly touch = output<void>();

  private onChange: (val: T) => void = () => {};
  private onTouched: () => void = () => {};

  selectOption(optVal: T): void {
    if (this.isDisabled()) return;
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

  setDisabledState(isDisabled: boolean): void {
    this.formDisabled.set(isDisabled);
  }
}
