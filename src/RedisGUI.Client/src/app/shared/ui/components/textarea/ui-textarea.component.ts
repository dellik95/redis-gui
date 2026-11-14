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

@Component({
  selector: 'ui-textarea',
  standalone: true,
  imports: [FormsModule, MatIconModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => UiTextareaComponent),
      multi: true
    }
  ],
  templateUrl: './ui-textarea.component.html',
  styleUrl: './ui-textarea.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UiTextareaComponent implements FormValueControl<string>, ControlValueAccessor {
  private static nextId = 0;
  readonly textareaId = `ui-textarea-${++UiTextareaComponent.nextId}`;

  readonly value = model<string>('');
  readonly label = input<string>();
  readonly placeholder = input<string>('');
  readonly rows = input<number>(4);
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

  readonly blurred = output<void>();
  readonly focused = output<void>();

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

  onInput(event: Event): void {
    const target = event.target as HTMLTextAreaElement;
    this.value.set(target.value);
    this.onChange(target.value);
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
