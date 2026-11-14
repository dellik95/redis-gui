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
import { FormCheckboxControl } from '@angular/forms/signals';

@Component({
  selector: 'ui-checkbox',
  standalone: true,
  imports: [],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiCheckboxComponent),
      multi: true
    }
  ],
  templateUrl: './ui-checkbox.component.html',
  styleUrl: './ui-checkbox.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiCheckboxComponent implements FormCheckboxControl, ControlValueAccessor {
  readonly checked = model<boolean>(false);
  readonly label = input<string>();
  readonly description = input<string>();
  readonly disabled = input<boolean>(false);
  private readonly formDisabled = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.formDisabled());

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
    if (!this.isDisabled()) {
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

  setDisabledState(isDisabled: boolean): void {
    this.formDisabled.set(isDisabled);
  }
}
