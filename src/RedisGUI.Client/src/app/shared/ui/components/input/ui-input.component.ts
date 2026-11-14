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
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'ui-input',
  standalone: true,
  imports: [FormsModule, MatIconModule, MatTooltipModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiInputComponent),
      multi: true
    }
  ],
  templateUrl: './ui-input.component.html',
  styleUrl: './ui-input.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiInputComponent implements FormValueControl<string | number>, ControlValueAccessor {
  private static nextId = 0;
  readonly inputId = `ui-input-${++UiInputComponent.nextId}`;

  readonly value = model<string | number>('');
  readonly label = input<string>();
  readonly placeholder = input<string>('');
  readonly type = input<string>('text');
  readonly icon = input<string>();
  readonly suffixIcon = input<string>();
  readonly clearable = input<boolean>(false);
  readonly disabled = input<boolean>(false);
  private readonly formDisabled = signal(false);
  readonly isDisabled = computed(() => this.disabled() || this.formDisabled());
  readonly readonly = input<boolean>(false);
  readonly required = input<boolean>(false);
  readonly monospace = input<boolean>(false);
  readonly error = input<string>();
  readonly hint = input<string>();
  readonly fullWidth = input<boolean>(true);

  // Signal Forms & Control Integration
  readonly name = input<string>('');
  readonly invalid = input<boolean>(false);
  readonly touched = input<boolean>(false);
  readonly dirty = input<boolean>(false);
  readonly errors = input<readonly any[]>([]);
  readonly touch = output<void>();

  readonly enter = output<void>();
  readonly cleared = output<void>();
  readonly blurred = output<void>();
  readonly focused = output<void>();
  readonly suffixClicked = output<void>();

  readonly isFocused = model(false);

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

  readonly hasValue = computed(() => {
    const val = this.value();
    return val !== null && val !== undefined && val !== '';
  });

  onInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    const val =
      this.type() === 'number' ? (target.value === '' ? '' : Number(target.value)) : target.value;
    this.value.set(val);
    this.onChange(val);
  }

  onFocus(): void {
    this.isFocused.set(true);
    this.focused.emit();
  }

  onBlur(): void {
    this.isFocused.set(false);
    this.onTouched();
    this.touch.emit();
    this.blurred.emit();
  }

  clear(event: MouseEvent): void {
    event.stopPropagation();
    this.value.set('');
    this.onChange('');
    this.cleared.emit();
  }

  // ControlValueAccessor
  writeValue(val: any): void {
    this.value.set(val ?? '');
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
